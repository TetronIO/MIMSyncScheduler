using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Management.Automation;
using System.Threading.Tasks;
using System.Xml.Linq;
using Tetron.Mim.SynchronisationScheduler.Models;

namespace Tetron.Mim.SynchronisationScheduler
{
    internal class Program
    {
        #region accessors
        /// <summary>
        /// Indicates whether or not any management agents that have run imports have resulted in any pending imports to the Metaverse.
        /// </summary>
        private static bool ManagementAgentImportsHadChanges { get; set; }
        private static bool InWhatIfMode { get; set; }
        /// <summary>
        /// A prefix to put at the front of logging instructions. When run in WHAT IF mode, a suitable prefix to indicate this will be returned, otherwise nothing.
        /// </summary>
        private static string LoggingPrefix => InWhatIfMode ? "WHATIF: " : string.Empty;

        /// <summary>
        /// If set to true, on a task not completing, processing of the schedule will stop and no more tasks will be processed.
        /// </summary>
        private static bool StopOnIncompletion { get; set; }
        #endregion

        #region public methods
        /// <summary>
        /// The master method. Runs when the program is run.
        /// </summary>
        private static void Main(string[] args)
        {
            // program needs to run MIM run profiles via WMI in a specific sequence, some in parallel.
            // run profiles should be multi-stepped to make control via the scheduler a simple as possible. 
            // run profiles should be able to be toggled via the configuration file to allow for operational flexibility.
            // when some run profiles are complete, additional steps need to be performed to ensure dependent synchronisation tasks occur and/or data delivery occurs.
            // program design must focus on simplicity and easy of reconfiguration.

            InitialiseLogging();
            Log.Information(LoggingPrefix + "Starting...");
            Log.Debug("------ Schedule execution starting... ------");

            try
            {
                var timer = new Timer();
                if (args == null || args.Length != 1)
                {
                    Log.Fatal(LoggingPrefix + "No schedule file path parameter supplied. Cannot continue.");
                    return;
                }

                if (ConfigurationManager.AppSettings["whatif"] != null)
                {
                    InWhatIfMode = bool.Parse(ConfigurationManager.AppSettings["whatif"]);
                    if (InWhatIfMode)
                        Log.Debug("WhatIfMode enabled.");
                }

                var schedule = LoadSchedule(args[0]);
                if (schedule == null)
                    return;

                ExecuteSchedule(schedule);
                Log.Debug("------ Schedule execution complete ------");

                Log.Information("Finished.");
                timer.Stop();
                if (!InWhatIfMode)
                    return;

                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Log.Error(ex, LoggingPrefix + "Unhandled exception: " + ex.Message);
            }
            finally
            {
                // ensure all logs are written to the outputs before exiting
                Log.CloseAndFlush();
            }
        }
        #endregion

        #region private methods
        private static void InitialiseLogging()
        {
            var configLoggingLevel = ConfigurationManager.AppSettings["LoggingLevel"];
            if (string.IsNullOrEmpty(configLoggingLevel))
                configLoggingLevel = "Verbose";

            var loggerConfiguration = new LoggerConfiguration();
            switch (configLoggingLevel.ToLower())
            {
                case "verbose":
                    loggerConfiguration.MinimumLevel.Verbose();
                    break;
                case "debug":
                    loggerConfiguration.MinimumLevel.Debug();
                    break;
                case "information":
                    loggerConfiguration.MinimumLevel.Information();
                    break;
                case "warning":
                    loggerConfiguration.MinimumLevel.Warning();
                    break;
                case "error":
                    loggerConfiguration.MinimumLevel.Error();
                    break;
                case "fatal":
                    loggerConfiguration.MinimumLevel.Fatal();
                    break;
                default:
                    loggerConfiguration.MinimumLevel.Information();
                    break;
            }

            Log.Logger = loggerConfiguration
                .WriteTo.Console()
                .WriteTo.Debug()
                .WriteTo.File("logs/scheduler-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        /// <summary>
        /// Loads a synchronisation schedule from file into memory.
        /// </summary>
        private static Schedule LoadSchedule(string scheduleFilePath)
        {
            var timer = new Timer();
            if (!File.Exists(scheduleFilePath))
            {
                Log.Fatal(LoggingPrefix + "Schedule file not found at the supplied location. Processing cannot continue.");
                timer.Stop();
                return null;
            }

            var schedule = new Schedule();
            var doc = XDocument.Load(scheduleFilePath);
            if (doc.Root == null || !doc.Root.Name.ToString().Equals("Schedule", StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Fatal(LoggingPrefix + "No root Schedule node found in the schedule file. Processing cannot continue.");
                timer.Stop();
                return null;
            }

            schedule.Name = doc.Root.Attribute("Name").Value;
            foreach (var node in doc.Root.Elements().Where(node => node.Attribute("Enabled").Value.Equals("true", StringComparison.InvariantCultureIgnoreCase)))
                schedule.Tasks.Add(BuildScheduleTask(node));

            if (doc.Root.Attribute("StopOnIncompletion") != null)
                schedule.StopOnIncompletion = bool.Parse(doc.Root.Attribute("StopOnIncompletion").Value);
            StopOnIncompletion = schedule.StopOnIncompletion;

            // validate blocks
            if (!ValidateBlockTasks(schedule.Tasks))
            {
                Log.Fatal(LoggingPrefix + "Block tasks found amongst non-block tasks. Block tasks must have block task siblings.");
                timer.Stop();
                return null;
            }

            timer.Stop();
            return schedule;
        }

        /// <summary>
        /// Recursive method to build a ScheduleTask for a schedule. Calls itself if sub-tasks as found.
        /// </summary>
        private static ScheduleTask BuildScheduleTask(XElement node)
        {
            var task = new ScheduleTask();
            if (node.Name.ToString().Equals("ManagementAgent", StringComparison.InvariantCultureIgnoreCase))
            {
                task.Name = node.Attribute("Name").Value;
                task.Type = ScheduleTaskType.ManagementAgent;
                if (node.Attribute("RunProfile") == null || string.IsNullOrEmpty(node.Attribute("RunProfile").Value))
                    throw new ConfigurationErrorsException("RunProfile attribute is either missing or has an invalid value for a ManagementAgent node.");
                task.Command = node.Attribute("RunProfile").Value;
                if (node.Attribute("OnlyIfPendingExportsExist") != null)
                    task.OnlyRunIfPendingExportsExist = node.Attribute("OnlyIfPendingExportsExist").Value.Equals("true", StringComparison.InvariantCultureIgnoreCase);
            }
            else if (node.Name.ToString().Equals("Executable", StringComparison.InvariantCultureIgnoreCase))
            {
                task.Name = node.Attribute("Name").Value;
                task.Type = ScheduleTaskType.Executable;

                if (node.Attribute("Command") == null || string.IsNullOrEmpty(node.Attribute("Command").Value))
                    throw new ConfigurationErrorsException("Command attribute is either missing or has an invalid value for a Executable node.");

                if (node.Attribute("Arguments") != null)
                    task.Arguments = node.Attribute("Arguments").Value;

                if (node.Attribute("ShowWindow") != null)
                    task.ShowExecutableWindow = bool.Parse(node.Attribute("ShowWindow").Value);
                
                task.Command = node.Attribute("Command").Value;
            }
            else if (node.Name.ToString().Equals("PowerShell", StringComparison.InvariantCultureIgnoreCase))
            {
                task.Name = node.Attribute("Name").Value;
                task.Type = ScheduleTaskType.PowerShell;
                if (node.Attribute("Path") == null || string.IsNullOrEmpty(node.Attribute("Path").Value))
                    throw new ConfigurationErrorsException("Path attribute is either missing or has an invalid value for a PowerShell node.");
                task.Command = node.Attribute("Path").Value;
            }
            else if (node.Name.ToString().Equals("VisualBasicScript", StringComparison.InvariantCultureIgnoreCase))
            {
                task.Name = node.Attribute("Name").Value;
                task.Type = ScheduleTaskType.VisualBasicScript;
                if (node.Attribute("Path") == null || string.IsNullOrEmpty(node.Attribute("Path").Value))
                    throw new ConfigurationErrorsException("Path attribute is either missing or has an invalid value for a VisualBasicScript node.");
                task.Command = node.Attribute("Path").Value;
            }
            else if (node.Name.ToString().Equals("ContinuationCondition", StringComparison.InvariantCultureIgnoreCase))
            {
                task.Name = "ContinuationCondition";
                task.Type = ScheduleTaskType.ContinuationCondition;
                if (node.Attribute("Type") != null && node.Attribute("Type").Value.Equals("ManagementAgentsHadImports", StringComparison.InvariantCultureIgnoreCase))
                    task.ConditionType = ContinuationConditionType.ManagementAgentsHadImports;
                else
                    throw new ConfigurationErrorsException("Either no value or an invalid value was supplied for tye type attribute on a ContinuationCondition node in the schedule file.");
            }
            else if (node.Name.ToString().Equals("SqlServer", StringComparison.InvariantCultureIgnoreCase))
            {
                task.Name = node.Attribute("Name").Value;
                task.Type = ScheduleTaskType.SqlServer;
                if (node.Attribute("Command") == null || string.IsNullOrEmpty(node.Attribute("Command").Value))
                    throw new ConfigurationErrorsException("Command attribute is either missing or has an invalid value for a SqlServer node.");
                if (node.Attribute("Server") == null || string.IsNullOrEmpty(node.Attribute("Server").Value))
                    throw new ConfigurationErrorsException("Server attribute is either missing or has an invalid value for a SqlServer node.");
                task.Command = node.Attribute("Command").Value;
                task.Server = node.Attribute("Server").Value;
            }
            else if (node.Name.ToString().Equals("Block", StringComparison.InvariantCultureIgnoreCase))
            {
                task.Name = node.Attribute("Name").Value;
                task.Type = ScheduleTaskType.Block;
            }

            foreach (var descendant in node.Elements().Where(q => q.Attribute("Enabled").Value.Equals("true", StringComparison.InvariantCultureIgnoreCase)))
                task.ChildTasks.Add(BuildScheduleTask(descendant));

            return task;
        }

        /// <summary>
        /// Kicks off the synchronisation schedule, executing MIM run profiles and any post-processing tasks.
        /// Performs steps in parallel where possible to minimise the total synchronisation time.
        /// </summary>
        private static void ExecuteSchedule(Schedule schedule)
        {
            // thought: should the scheduler run continuously for a set period and try and run the supplied schedule
            // as often as possible, whilst respecting an optional minimal interval period, or leave it up to a Windows Scheduled Task
            // to call the scheduler every now and then? Going with the latter for now.

            // process:
            // -- run non-block tasks in parallel.
            // -- run block tasks after other blocks, i.e. in sequence.
            // -- once a task is run, execute child tasks (respecting any parallel/sequential execution). This will cause the desired recursion.
            // notes:
            // -- if a task fails then child tasks should not be executed.

            var timer = new Timer();
            Log.Information($"{LoggingPrefix}Running schedule '{schedule.Name}'. Stopping on incomplete tasks: {schedule.StopOnIncompletion}");
            ExecuteTasks(schedule.Tasks);
            timer.Stop();
        }

        /// <summary>
        /// Executes a list of schedule tasks, either in parallel (by default) or in sequence (blocks).
        /// If false is returned, no more schedule processing should occur.
        /// </summary>
        private static bool ExecuteTasks(List<ScheduleTask> tasks)
        {
            // tasks are executed in parallel by default, though block task types are executed in sequence to help preserve dependencies.
            if (tasks.All(q => q.Type == ScheduleTaskType.Block))
            {
                // ReSharper would rewrite the foreach, so it's less clear it's a loop we want to stop at the first instance of an issue. don't let it!
                foreach (var task in tasks.Where(task => !ExecuteScheduleTask(task)))
                {
                    Log.Warning($"Not continuing to process the schedule due to the task '{task.Name}' not completing and the schedule being set to stop on errors.");
                    return false;
                }
            }
            else
            {
                Parallel.ForEach(tasks, task =>
                {
                    try
                    {
                        // as all tasks are executed in parallel, there's no point breaking out of this loop if an errors is encountered.
                        // any child tasks present will not be executed in such a scenario which is probably the most preferable behaviour.
                        ExecuteScheduleTask(task);
                    }
                    catch (Exception ex)
                    {
                        // exceptions need to be caught here due to them occurring on a new thread which stops execution bubbling as part of the Parallel loop.
                        Log.Error(ex, $"{LoggingPrefix}task loop - Unhandled exception");
                    }
                });
            }

            // do we have any tasks to retry sequentially?
            foreach (var task in tasks.Where(q => q.RetryRequired))
            {
                Log.Debug($"Retrying task '{task.ToString()}' sequentially.");
                ExecuteScheduleTask(task);
            }

            return true;
        }

        /// <summary>
        /// Recursive method to execute a schedule task and when complete and successful, execute all child tasks in parallel.
        /// </summary>
        private static bool ExecuteScheduleTask(ScheduleTask task)
        {
            var shouldTaskRun = true;
            var taskComplete = false;
            switch (task.Type)
            {
                case ScheduleTaskType.ManagementAgent:
                    if (task.OnlyRunIfPendingExportsExist)
                    {
                        Log.Information($"{LoggingPrefix}Evaluating potential execution of '{task.Name}' Management Agent, run profile: '{task.Command}'");
                        shouldTaskRun = PendingExportsInManagementAgent(task.Name);
                    }
                    if (shouldTaskRun)
                    {
                        Log.Information($"{LoggingPrefix}Executing the '{task.Name}' Management Agent, run profile: '{task.Command}'");
                        taskComplete = ExecuteMimRunProfile(task.Name, task.Command, out var retryRequired);
                        if (retryRequired) task.RetryRequired = true;
                    }
                    break;
                case ScheduleTaskType.PowerShell:
                    Log.Information($"{LoggingPrefix}Executing '{task.Name}' PowerShell script: '{task.Command}'");
                    taskComplete = ExecutePowerShellScript(task.Command);
                    break;
                case ScheduleTaskType.VisualBasicScript:
                    Log.Information($"{LoggingPrefix}Executing '{task.Name}' Visual Basic script: '{task.Command}'");
                    taskComplete = ExecuteVisualBasicScript(task.Command);
                    break;
                case ScheduleTaskType.Executable:
                    Log.Information($"{LoggingPrefix}Executing '{task.Name}' executable: '{task.Command}'");
                    taskComplete = ExecuteExecutable(task.Command, task.Arguments, task.ShowExecutableWindow);
                    break;
                case ScheduleTaskType.SqlServer:
                    Log.Information($"{LoggingPrefix}Executing '{task.Name}' sql server command: '{task.Command}' on server: '{task.Server}'");
                    taskComplete = ExecuteSqlServerCommand(task.Command, task.Server);
                    break;
                case ScheduleTaskType.Block:
                    Log.Information($"{LoggingPrefix}Executing block '{task.Name}'");
                    taskComplete = true;
                    break;
                case ScheduleTaskType.ContinuationCondition:
                    if (task.ConditionType == ContinuationConditionType.ManagementAgentsHadImports && !ManagementAgentImportsHadChanges)
                    {
                        Log.Information(LoggingPrefix + "ContinuationCondition:ManagementAgentsHadImports - No pending imports detected. Task: " + task.ToString());
                        shouldTaskRun = false;
                    }
                    else
                    {
                        taskComplete = true;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            #region validation
            if (!taskComplete && StopOnIncompletion)
                return false;

            if (!shouldTaskRun)
            {
                Log.Information(LoggingPrefix + "Logic has determined this and any child tasks should not run. Task: " + task.ToString());
                return false;
            }

            if (taskComplete || task.ChildTasks.Count <= 0) return ExecuteTasks(task.ChildTasks);
            Log.Warning(LoggingPrefix + "Not executing child tasks due to the last error event. Current task: " + task.ToString());
            return false;
            #endregion
        }

        /// <summary>
        /// Instructs the MIM Synchronisation Engine to execute a particular run profile against a particular management agent.
        /// </summary>
        /// <param name="managementAgentName">The name of the MIM management agent to execute a run profile against.</param>
        /// <param name="runProfileName">The name of the </param>
        /// <param name="retryRequired">If set to true, this run profile should be re-executed again on its own (could be related to a sql-deadlock result).</param>
        /// <returns>A boolean indicating whether the run profile completed (i.e. did not encounter a fatal error).</returns>
        private static bool ExecuteMimRunProfile(string managementAgentName, string runProfileName, out bool retryRequired)
        {
            try
            {
                if (InWhatIfMode)
                {
                    // debug mode only shows what would happen if we were not in debug mode. no actions are to be performed.
                    Log.Debug($"{LoggingPrefix}Executing run profile: {managementAgentName}\\{runProfileName}");
                    if (runProfileName.StartsWith("DISO", StringComparison.InvariantCultureIgnoreCase) || runProfileName.StartsWith("FISO", StringComparison.InvariantCultureIgnoreCase))
                        ManagementAgentImportsHadChanges = true;

                    retryRequired = false;
                    return true;
                }

                var timer = new Timer();
                const string mimSyncServiceMaObjectSpace = "MIIS_ManagementAgent.Name";
                const string mimSyncServiceWmiNameSpace = "root\\MicrosoftIdentityIntegrationServer";

                var managementObject = new ManagementObject(
                    mimSyncServiceWmiNameSpace,
                    $"{mimSyncServiceMaObjectSpace}='{managementAgentName}'",
                    null);

                var inParameters = managementObject.GetMethodParameters("Execute");
                inParameters["RunProfileName"] = runProfileName;
                var result = managementObject.InvokeMethod("Execute", inParameters, null);
                if (result != null)
                    foreach (var property in result.Properties)
                        Log.Information($"MA: {managementAgentName}, run profile: {runProfileName}, result property: {property.Name}, value: {property.Value}");

                // generally any management agent status starting with "stopped-" is a fatal error and not something we should continue processing any child tasks from.
                // errors less severe than this are data related and should not stop synchronisation.
                
                // extending the bad response of 'stopped-*' as a catch for no response, which is also bad.
                var returnValue = result != null ? result.Properties["ReturnValue"].Value.ToString() : "stopped";
                Log.Information($"MA: {managementAgentName}, Run profile: {runProfileName}, ReturnValue: {returnValue}");
                var goodResponse = !(returnValue.StartsWith("stopped") || returnValue.StartsWith("call-failure:") || returnValue.StartsWith("no-start-") || returnValue.Equals("sql-deadlock"));

                // these responses are actually acceptable but have been marked as bad already above.
                if (returnValue.Equals("stopped-user-termination-from-wmi-or-ui") || returnValue.Equals("stopped-object-limit"))
                    goodResponse = true;

                if (goodResponse)
                {
                    // if this was an import run profile then we need to see if it resulted in any pending imports
                    // as this may be used later in the schedule to determine whether to continue processing.
                    // dependency: requires a convention of DISO* or FISO* to represent delta/full-import run profiles -- we could just do this for all operations but this might slow the schedule down.
                    if (runProfileName.StartsWith("DISO", StringComparison.InvariantCultureIgnoreCase) || runProfileName.StartsWith("FISO", StringComparison.InvariantCultureIgnoreCase))
                        if (!ManagementAgentImportsHadChanges && PendingImportsInManagementAgent(managementAgentName))
                            ManagementAgentImportsHadChanges = true;
                }
                else if (returnValue.Equals("sql-deadlock"))
                {
                    // in this scenario we should retry the task. this is commonly experienced when other run profiles are executed in parallel and some weird page-locking
                    // happens on the sync engine db (why does it use page locking and not row locking???).
                    // when this happens we should mark the task as needing a retry, the scheduler will then retry this task on its own, which is why we can't just retry it here
                    // as we could experience it again if other tasks are executing.
                    retryRequired = true;
                    return false;
                }

                retryRequired = false;
                timer.Stop();
                return goodResponse;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{LoggingPrefix}Unhandled exception ({managementAgentName}\\{runProfileName}): {ex.Message}");
                retryRequired = false;
                return false;
            }
        }

        /// <summary>
        /// Executes a PowerShell script and returns a success/fail response.
        /// </summary>
        /// <param name="scriptPath">The full path to the PowerShell script.</param>
        /// <returns>A boolean indicating whether the script returned a successful response.</returns>
        private static bool ExecutePowerShellScript(string scriptPath)
        {
            if (InWhatIfMode)
            {
                // debug mode only shows what would happen if we were not in debug mode. no actions are to be performed.
                Log.Debug($"{LoggingPrefix}Executing PowerShell script: {scriptPath}");
                return true;
            }
            
            try
            {
                var timer = new Timer();
                using var shell = PowerShell.Create();
                shell.AddCommand("Set-ExecutionPolicy").AddArgument("Unrestricted").AddParameter("Scope", "CurrentUser");
                shell.Commands.AddScript(scriptPath);

                // we need to subscribe to these event handlers so we can get progress of the PowerShell script out into our logs
                shell.Streams.Debug.DataAdded += PowerShellDebugStreamHandler;
                shell.Streams.Verbose.DataAdded += PowerShellVerboseStreamHandler;
                shell.Streams.Information.DataAdded += PowerShellInformationStreamHandler;
                shell.Streams.Warning.DataAdded += PowerShellWarningStreamHandler;
                shell.Streams.Error.DataAdded += PowerShellErrorStreamHandler;

                var results = shell.Invoke<string>();
                if (results == null || results.Count == 0)
                    return true;

                // we used to watch for a 'success' response, but this isn't required anymore
                // but we might still want to inspect the output in the future, so leaving this logging in.
                foreach (var result in results)
                    Log.Debug($"{LoggingPrefix}PowerShell output: {result}");

                timer.Stop();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{LoggingPrefix}Unhandled exception when executing PowerShell task: {scriptPath}");
                return false;
            }
        }

        /// <summary>
        /// Executes a Visual Basic script and returns a success/fail response.
        /// </summary>
        /// <param name="scriptPath">The full path to the vbs script.</param>
        /// <returns>A boolean indicating whether the script returned a successful response.</returns>
        private static bool ExecuteVisualBasicScript(string scriptPath)
        {
            if (InWhatIfMode)
            {
                // debug mode only shows what would happen if we were not in debug mode. no actions are to be performed.
                Log.Debug($"{LoggingPrefix}Executing Visual Basic script: {scriptPath}");
                return true;
            }

            try
            {
                var timer = new Timer();
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = "cscript", 
                        Arguments = $"/Nologo \"{scriptPath}\"", 
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    }
                };
                process.OutputDataReceived += VbsOutputDataReceivedHandler;
                process.ErrorDataReceived += VbsErrorDataReceivedHandler;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                timer.Stop();
                return (Utilities.SynchronisationTaskExitCode)process.ExitCode == Utilities.SynchronisationTaskExitCode.Success;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{LoggingPrefix}Unhandled exception when executing vbs task: {scriptPath}");
                return false;
            }
        }

        /// <summary>
        /// Executes an arbitrary external command, i.e. an .exe file. Looks for a "success" response at the end of the
        /// executable output to determine if the process was successful.
        /// </summary>
        /// <param name="executablePath">The full path to the executable.</param>
        /// <param name="arguments">Any arguments needed to accompany the executable path.</param>
        /// <param name="showWindow">Determines whether the executable window will be shown to the logged-in user.</param>
        private static bool ExecuteExecutable(string executablePath, string arguments, bool showWindow)
        {
            if (InWhatIfMode)
            {
                // whatif mode only shows what would happen if we were not in whatif mode. no actions are to be performed.
                Log.Debug($"{LoggingPrefix}Executing executable: {executablePath}");
                return true;
            }

            try
            {
                var timer = new Timer();
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = executablePath,
                        Arguments = arguments,
                        CreateNoWindow = !showWindow,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    }
                };
                process.OutputDataReceived += ExecutableOutputDataReceivedHandler;
                process.ErrorDataReceived += ExecutableErrorDataReceivedHandler;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                timer.Stop();

                return (Utilities.SynchronisationTaskExitCode) process.ExitCode == Utilities.SynchronisationTaskExitCode.Success;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{LoggingPrefix}Unhandled exception when executing '{executablePath}'");
                return false;
            }
        }

        /// <summary>
        /// Executes an MS SQL Server command against a specific host. No response is returned.
        /// Use "EXEC ..." for executing stored procedures.
        /// Use fully qualified references to objects, i.e. 'database.schema.object'.
        /// </summary>
        private static bool ExecuteSqlServerCommand(string command, string server)
        {
            if (InWhatIfMode)
            {
                // debug mode only shows what would happen if we were not in debug mode. no actions are to be performed.
                Log.Debug($"{LoggingPrefix}Executing Sql Server command: '{command}' on '{server}'");
                return true;
            }

            try
            {
                var timer = new Timer();
                var connectionString = new SqlConnectionStringBuilder { DataSource = server, IntegratedSecurity = true };
                Log.Information($"SQL Server connection string: {connectionString}");
                using (var connection = new SqlConnection(connectionString.ToString()))
                using (var sqlCommand = new SqlCommand(command, connection))
                {
                    connection.Open();
                    sqlCommand.CommandTimeout = 300;
                    sqlCommand.ExecuteNonQuery();
                    Log.Information($"SQL Server command executed: {command}");
                }
                timer.Stop();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{LoggingPrefix}SQL Server unhandled exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Determines whether there are any pending exports on a management agent.
        /// Useful for determining if an export should take place or not and any dependent actions.
        /// </summary>
        /// <param name="managementAgentName">The name of the Management Agent as seen in the Synchronisation Manager.</param>
        private static bool PendingExportsInManagementAgent(string managementAgentName)
        {
            if (InWhatIfMode)
                return true;

            var timer = new Timer();
            var scope = new ManagementScope(@"root\MicrosoftIdentityIntegrationServer");
            var query = new SelectQuery($"Select * from MIIS_ManagementAgent where name ='{managementAgentName}'");
            var searcher = new ManagementObjectSearcher(scope, query);
            ManagementObject wmiMaObject = null;

            foreach (var obj in searcher.Get().Cast<ManagementObject>())
                wmiMaObject = obj;

            if (wmiMaObject == null)
            {
                Log.Warning(LoggingPrefix + "Couldn't get a response of any back; going to be conservative and return true.");
                return true;
            }

            var pendingExports = 0;
            pendingExports += int.Parse(wmiMaObject.InvokeMethod("NumExportAdd", new object[] { }).ToString());
            pendingExports += int.Parse(wmiMaObject.InvokeMethod("NumExportUpdate", new object[] { }).ToString());
            pendingExports += int.Parse(wmiMaObject.InvokeMethod("NumExportDelete", new object[] { }).ToString());

            timer.Stop();
            return pendingExports > 0;
        }

        /// <summary>
        /// Determines whether there are any pending imports on a management agent.
        /// Useful for determining if a synchronisation should take place or not and any dependent actions.
        /// </summary>
        /// <param name="managementAgentName">The name of the Management Agent as seen in the Synchronisation Manager.</param>
        private static bool PendingImportsInManagementAgent(string managementAgentName)
        {
            if (InWhatIfMode)
                return true;

            var timer = new Timer();
            var scope = new ManagementScope(@"root\MicrosoftIdentityIntegrationServer");
            var query = new SelectQuery($"Select * from MIIS_ManagementAgent where name ='{managementAgentName}'");
            var searcher = new ManagementObjectSearcher(scope, query);
            ManagementObject wmiMaObject = null;

            foreach (var obj in searcher.Get().Cast<ManagementObject>())
                wmiMaObject = obj;

            if (wmiMaObject == null)
            {
                Log.Warning(LoggingPrefix + "Couldn't get a response of any back; going to be conservative and return true.");
                return true;
            }

            var pendingExports = 0;
            pendingExports += int.Parse(wmiMaObject.InvokeMethod("NumImportAdd", new object[] { }).ToString());
            pendingExports += int.Parse(wmiMaObject.InvokeMethod("NumImportUpdate", new object[] { }).ToString());
            pendingExports += int.Parse(wmiMaObject.InvokeMethod("NumImportDelete", new object[] { }).ToString());

            timer.Stop();
            return pendingExports > 0;
        }
        #endregion

        #region helpers
        /// <summary>
        /// Validates a list of tasks to ensure any blocks have only block siblings.
        /// </summary>
        /// <returns>True if no block tasks found or if any block tasks have only block siblings, otherwise false.</returns>
        private static bool ValidateBlockTasks(List<ScheduleTask> tasks)
        {
            if (!(tasks.All(q => q.Type != ScheduleTaskType.Block) || tasks.All(q => q.Type == ScheduleTaskType.Block)))
                return false;
            return tasks.All(task => ValidateBlockTasks(task.ChildTasks));
        }
        #endregion

        #region event handlers
        private static void PowerShellVerboseStreamHandler(object sender, DataAddedEventArgs ea)
        {
            if (sender is PSDataCollection<VerboseRecord> streamObjectsReceived)
            {
                var currentStreamRecord = streamObjectsReceived[ea.Index];
                Log.Verbose($"{LoggingPrefix}PowerShell: {currentStreamRecord.Message}");
            }
        }

        private static void PowerShellDebugStreamHandler(object sender, DataAddedEventArgs ea)
        {
            if (sender is PSDataCollection<DebugRecord> streamObjectsReceived)
            {
                var currentStreamRecord = streamObjectsReceived[ea.Index];
                Log.Debug($"{LoggingPrefix}PowerShell: {currentStreamRecord.Message}");
            }
        }

        private static void PowerShellInformationStreamHandler(object sender, DataAddedEventArgs ea)
        {
            if (sender is PSDataCollection<InformationRecord> streamObjectsReceived)
            {
                var currentStreamRecord = streamObjectsReceived[ea.Index];
                if (currentStreamRecord.MessageData is HostInformationMessage hostInformationMessage)
                {
                    Log.Information($"{LoggingPrefix}PowerShell: {hostInformationMessage.Message}");
                }
            }
        }

        private static void PowerShellWarningStreamHandler(object sender, DataAddedEventArgs ea)
        {
            if (sender is PSDataCollection<WarningRecord> streamObjectsReceived)
            {
                var currentStreamRecord = streamObjectsReceived[ea.Index];
                Log.Warning($"{LoggingPrefix}PowerShell: {currentStreamRecord.Message}");
            }
        }

        private static void PowerShellErrorStreamHandler(object sender, DataAddedEventArgs ea)
        {
            if (sender is PSDataCollection<ErrorRecord> streamObjectsReceived)
            {
                var currentStreamRecord = streamObjectsReceived[ea.Index];
                if (currentStreamRecord.ErrorDetails != null)
                    Log.Error(currentStreamRecord.Exception, $"{LoggingPrefix}PowerShell: {currentStreamRecord.ErrorDetails.Message}");
                else
                    Log.Error(currentStreamRecord.Exception, $"{LoggingPrefix}PowerShell: {currentStreamRecord.Exception.Message}");
            }
        }

        private static void VbsErrorDataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            if (e != null && !string.IsNullOrEmpty(e.Data))
                Log.Error($"{LoggingPrefix}VBS: {e.Data}");
        }

        private static void VbsOutputDataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            Log.Debug($"{LoggingPrefix}VBS: {e.Data}");
        }

        private static void ExecutableErrorDataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            if (e != null && !string.IsNullOrEmpty(e.Data))
                Log.Error($"{LoggingPrefix}Executable: {e.Data}");
        }

        private static void ExecutableOutputDataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            Log.Debug($"{LoggingPrefix}Executable: {e.Data}");
        }
        #endregion
    }
}

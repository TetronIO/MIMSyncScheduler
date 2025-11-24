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
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;
using System.Xml.Linq;
using Tetron.Mim.SynchronisationScheduler.Models;

namespace Tetron.Mim.SynchronisationScheduler
{
    internal static class Program
    {
        /// <summary>
        /// The master method. Runs when the program is run.
        /// </summary>
        private static void Main(string[] args)
        {
            InitialiseLogging();

            try
            {
                var timer = new Timer();

                // Validate arguments
                if (!(args is { Length: 1 }))
                {
                    Log.Fatal("No schedule file path parameter supplied. Cannot continue.");
                    return;
                }

                // Determine WhatIf mode
                var whatIfMode = false;
                if (ConfigurationManager.AppSettings["whatif"] != null)
                {
                    whatIfMode = bool.Parse(ConfigurationManager.AppSettings["whatif"]);
                    if (whatIfMode)
                        Log.Debug("WhatIfMode enabled.");
                }

                var loggingPrefix = whatIfMode ? "WHATIF: " : string.Empty;
                Log.Information($"{loggingPrefix}Starting...");
                Log.Debug("------ Schedule execution starting... ------");

                // Load schedule
                var schedule = LoadSchedule(args[0]);
                if (schedule == null)
                    return;

                // Create services with dependency injection
                var processExecutor = new Services.ProcessExecutor(whatIfMode);
                var sqlExecutor = new Services.SqlExecutor(whatIfMode);
                var managementAgentExecutor = new Services.ManagementAgentExecutor(whatIfMode);
                var taskExecutor = new Services.TaskExecutor(processExecutor, sqlExecutor, managementAgentExecutor, whatIfMode);
                var scheduleExecutor = new Services.ScheduleExecutor(taskExecutor, whatIfMode);

                // Execute schedule
                scheduleExecutor.ExecuteSchedule(schedule);

                Log.Debug("------ Schedule execution complete ------");
                Log.Information("Finished.");
                timer.Stop();

                if (whatIfMode)
                {
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unhandled exception: {ex.Message}");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

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

            // Determine log file mode
            var logFileMode = ConfigurationManager.AppSettings["LogFileMode"];
            if (string.IsNullOrEmpty(logFileMode))
                logFileMode = "Daily";

            string logFilePath;
            switch (logFileMode.ToLower())
            {
                case "perexecution":
                    // Format: YYYYMMDDHHmmss-scheduler.log
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    logFilePath = $"logs/{timestamp}-scheduler.log";
                    Log.Logger = loggerConfiguration
                        .WriteTo.Console()
                        .WriteTo.Debug()
                        .WriteTo.File(logFilePath)
                        .CreateLogger();
                    break;
                case "daily":
                default:
                    // Format: YYYYMMDD-scheduler.log (shared for all executions on same day)
                    var dateStamp = DateTime.Now.ToString("yyyyMMdd");
                    logFilePath = $"logs/{dateStamp}-scheduler.log";
                    Log.Logger = loggerConfiguration
                        .WriteTo.Console()
                        .WriteTo.Debug()
                        .WriteTo.File(logFilePath)
                        .CreateLogger();
                    break;
            }
        }

        /// <summary>
        /// Loads a synchronisation schedule from file into memory.
        /// </summary>
        private static Schedule LoadSchedule(string scheduleFilePath)
        {
            var timer = new Timer();
            if (!File.Exists(scheduleFilePath))
            {
                Log.Fatal("Schedule file not found at the supplied location. Processing cannot continue.");
                timer.Stop();
                return null;
            }

            var schedule = new Schedule();
            var doc = XDocument.Load(scheduleFilePath);
            if (doc.Root == null || !doc.Root.Name.ToString().Equals("Schedule", StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Fatal("No root Schedule node found in the schedule file. Processing cannot continue.");
                timer.Stop();
                return null;
            }

            var nameAttr = doc.Root.Attribute("Name");
            if (nameAttr == null || string.IsNullOrEmpty(nameAttr.Value))
            {
                Log.Fatal("No Name attribute found on Schedule element. Processing cannot continue.");
                timer.Stop();
                return null;
            }
            schedule.Name = nameAttr.Value;

            foreach (var node in doc.Root.Elements().Where(node => node.Attribute("Enabled").Value.Equals("true", StringComparison.InvariantCultureIgnoreCase)))
                schedule.Tasks.Add(BuildScheduleTask(node));

            if (doc.Root.Attribute("StopOnIncompletion") != null)
                schedule.StopOnIncompletion = bool.Parse(doc.Root.Attribute("StopOnIncompletion").Value);

            // validate blocks
            if (!ValidateBlockTasks(schedule.Tasks))
            {
                Log.Fatal("Block tasks found amongst non-block tasks. Block tasks must have block task siblings.");
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
                    throw new ConfigurationErrorsException("Either no value or an invalid value was supplied for the type attribute on a ContinuationCondition node in the schedule file.");
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
        /// Validates a list of tasks to ensure any blocks have only block siblings.
        /// </summary>
        /// <returns>True if no block tasks found or if any block tasks have only block siblings, otherwise false.</returns>
        private static bool ValidateBlockTasks(List<ScheduleTask> tasks)
        {
            if (!(tasks.All(q => q.Type != ScheduleTaskType.Block) || tasks.All(q => q.Type == ScheduleTaskType.Block)))
                return false;
            return tasks.All(task => ValidateBlockTasks(task.ChildTasks));
        }
    }
}

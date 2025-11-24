using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tetron.Mim.SynchronisationScheduler.Interfaces;
using Tetron.Mim.SynchronisationScheduler.Models;

namespace Tetron.Mim.SynchronisationScheduler.Services
{
    /// <summary>
    /// Executes schedule tasks using injected dependencies.
    /// </summary>
    public class TaskExecutor : ITaskExecutor
    {
        private readonly IProcessExecutor _processExecutor;
        private readonly ISqlExecutor _sqlExecutor;
        private readonly IManagementAgentExecutor _managementAgentExecutor;
        private readonly string _loggingPrefix;

        public TaskExecutor(
            IProcessExecutor processExecutor,
            ISqlExecutor sqlExecutor,
            IManagementAgentExecutor managementAgentExecutor,
            bool whatIfMode = false)
        {
            _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
            _sqlExecutor = sqlExecutor ?? throw new ArgumentNullException(nameof(sqlExecutor));
            _managementAgentExecutor = managementAgentExecutor ?? throw new ArgumentNullException(nameof(managementAgentExecutor));
            _loggingPrefix = whatIfMode ? "WHATIF: " : string.Empty;
        }

        /// <inheritdoc />
        public bool ExecuteTask(ScheduleTask task, bool stopOnIncompletion, ref bool managementAgentImportsHadChanges)
        {
            var shouldTaskRun = true;
            var taskComplete = false;

            switch (task.Type)
            {
                case ScheduleTaskType.ManagementAgent:
                    if (task.OnlyRunIfPendingExportsExist)
                    {
                        Log.Information($"{_loggingPrefix}Evaluating potential execution of '{task.Name}' Management Agent, run profile: '{task.Command}'");
                        shouldTaskRun = _managementAgentExecutor.HasPendingExports(task.Name);
                    }
                    if (shouldTaskRun)
                    {
                        Log.Information($"{_loggingPrefix}Executing the '{task.Name}' Management Agent, run profile: '{task.Command}'");
                        taskComplete = _managementAgentExecutor.ExecuteRunProfile(task.Name, task.Command, out var retryRequired);
                        if (retryRequired) task.RetryRequired = true;

                        // Check if this was an import run profile and if it resulted in pending imports
                        if (taskComplete &&
                            (task.Command.StartsWith("DISO", StringComparison.InvariantCultureIgnoreCase) ||
                             task.Command.StartsWith("FISO", StringComparison.InvariantCultureIgnoreCase)))
                        {
                            if (!managementAgentImportsHadChanges && _managementAgentExecutor.HasPendingImports(task.Name))
                            {
                                managementAgentImportsHadChanges = true;
                            }
                        }
                    }
                    break;

                case ScheduleTaskType.PowerShell:
                    Log.Information($"{_loggingPrefix}Executing '{task.Name}' PowerShell script: '{task.Command}'");
                    taskComplete = _processExecutor.ExecutePowerShellScript(task.Command);
                    break;

                case ScheduleTaskType.VisualBasicScript:
                    Log.Information($"{_loggingPrefix}Executing '{task.Name}' Visual Basic script: '{task.Command}'");
                    taskComplete = _processExecutor.ExecuteVisualBasicScript(task.Command);
                    break;

                case ScheduleTaskType.Executable:
                    Log.Information($"{_loggingPrefix}Executing '{task.Name}' executable: '{task.Command}'");
                    taskComplete = _processExecutor.ExecuteExecutable(task.Command, task.Arguments, task.ShowExecutableWindow);
                    break;

                case ScheduleTaskType.SqlServer:
                    Log.Information($"{_loggingPrefix}Executing '{task.Name}' sql server command: '{task.Command}' on server: '{task.Server}'");
                    taskComplete = _sqlExecutor.ExecuteCommand(task.Command, task.Server);
                    break;

                case ScheduleTaskType.Block:
                    Log.Information($"{_loggingPrefix}Executing block '{task.Name}'");
                    taskComplete = true;
                    break;

                case ScheduleTaskType.ContinuationCondition:
                    if (task.ConditionType == ContinuationConditionType.ManagementAgentsHadImports && !managementAgentImportsHadChanges)
                    {
                        Log.Information($"{_loggingPrefix}ContinuationCondition:ManagementAgentsHadImports - No pending imports detected. Task: {task}");
                        shouldTaskRun = false;
                    }
                    else
                    {
                        taskComplete = true;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(task.Type), task.Type, "Unknown task type");
            }

            // Validation
            if (!taskComplete && stopOnIncompletion)
                return false;

            if (!shouldTaskRun)
            {
                Log.Information($"{_loggingPrefix}Logic has determined this and any child tasks should not run. Task: {task}");
                return false;
            }

            if (taskComplete)
            {
                return task.ChildTasks.Count > 0
                    ? ExecuteTasks(task.ChildTasks, stopOnIncompletion, ref managementAgentImportsHadChanges)
                    : true;
            }

            Log.Warning($"{_loggingPrefix}Not executing child tasks due to the last error event. Current task: {task}");
            return false;
        }

        /// <inheritdoc />
        public bool ExecuteTasks(List<ScheduleTask> tasks, bool stopOnIncompletion, ref bool managementAgentImportsHadChanges)
        {
            if (tasks == null || tasks.Count == 0)
                return true;

            // Check if these are block tasks (parallel execution)
            var isBlockTasks = tasks.All(t => t.Type == ScheduleTaskType.Block);

            if (isBlockTasks)
            {
                return ExecuteBlockTasks(tasks, stopOnIncompletion, ref managementAgentImportsHadChanges);
            }

            // Sequential execution
            foreach (var task in tasks)
            {
                var taskSuccess = ExecuteTask(task, stopOnIncompletion, ref managementAgentImportsHadChanges);

                if (!taskSuccess)
                {
                    if (task.RetryRequired)
                    {
                        Log.Warning($"{_loggingPrefix}Retrying task: {task}");
                        task.RetryRequired = false;
                        taskSuccess = ExecuteTask(task, stopOnIncompletion, ref managementAgentImportsHadChanges);
                    }

                    if (!taskSuccess && stopOnIncompletion)
                    {
                        Log.Warning($"{_loggingPrefix}Task failed and StopOnIncompletion is enabled. Stopping execution. Task: {task}");
                        return false;
                    }
                }
            }

            return true;
        }

        private bool ExecuteBlockTasks(List<ScheduleTask> blockTasks, bool stopOnIncompletion, ref bool managementAgentImportsHadChanges)
        {
            var allTasksSucceeded = true;
            var parallelTasks = new List<Task<(bool success, bool importsChanged)>>();

            // Execute all block tasks in parallel
            foreach (var task in blockTasks)
            {
                var localTask = task;

                var parallelTask = Task.Run(() =>
                {
                    var localImportsHadChanges = false;
                    var success = ExecuteTask(localTask, stopOnIncompletion, ref localImportsHadChanges);
                    return (success, localImportsHadChanges);
                });

                parallelTasks.Add(parallelTask);
            }

            // Wait for all to complete
            Task.WaitAll(parallelTasks.ToArray());

            // Check results and aggregate import changes
            foreach (var task in parallelTasks)
            {
                var (success, importsChanged) = task.Result;

                if (importsChanged)
                    managementAgentImportsHadChanges = true;

                if (!success)
                {
                    allTasksSucceeded = false;
                    if (stopOnIncompletion)
                    {
                        Log.Warning($"{_loggingPrefix}A block task failed and StopOnIncompletion is enabled.");
                        return false;
                    }
                }
            }

            return allTasksSucceeded;
        }
    }
}

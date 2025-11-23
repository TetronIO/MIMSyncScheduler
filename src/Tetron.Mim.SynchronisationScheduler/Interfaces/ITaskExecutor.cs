using System.Collections.Generic;
using Tetron.Mim.SynchronisationScheduler.Models;

namespace Tetron.Mim.SynchronisationScheduler.Interfaces
{
    /// <summary>
    /// Interface for executing schedule tasks.
    /// </summary>
    public interface ITaskExecutor
    {
        /// <summary>
        /// Executes a single schedule task.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        /// <param name="stopOnIncompletion">Whether to stop if the task doesn't complete successfully.</param>
        /// <param name="managementAgentImportsHadChanges">Tracks whether any MA imports have had changes.</param>
        /// <returns>True if the task completed successfully, false otherwise.</returns>
        bool ExecuteTask(ScheduleTask task, bool stopOnIncompletion, ref bool managementAgentImportsHadChanges);

        /// <summary>
        /// Executes a list of tasks.
        /// </summary>
        /// <param name="tasks">The list of tasks to execute.</param>
        /// <param name="stopOnIncompletion">Whether to stop if any task doesn't complete successfully.</param>
        /// <param name="managementAgentImportsHadChanges">Tracks whether any MA imports have had changes.</param>
        /// <returns>True if all tasks completed successfully, false otherwise.</returns>
        bool ExecuteTasks(List<ScheduleTask> tasks, bool stopOnIncompletion, ref bool managementAgentImportsHadChanges);
    }
}

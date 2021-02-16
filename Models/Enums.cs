namespace Tetron.Mim.SynchronisationScheduler.Models
{
    /// <summary>
    /// Defines what type of action a ScheduleTask is to perform.
    /// </summary>
    public enum ScheduleTaskType
    {
        ManagementAgent,
        Executable,
        PowerShell,
        /// <summary>
        /// Represents a condition for a decision to be made on whether or not to process child tasks.
        /// </summary>
        ContinuationCondition,
        /// <summary>
        /// Represents a command executed against a MS SQL Server instance.
        /// </summary>
        SqlServer,
        /// <summary>
        /// Block task types are executed in sequence as opposed to in parallel. This allows for dependencies between tasks to be honoured.
        /// Block tasks must be preceded and succeeded by zero or more Block types. An exception will be thrown if any other type is a sibling.
        /// </summary>
        Block
    }

    /// <summary>
    /// Defines a type of ContinuationCondition, which itself it used to check if the schedule
    /// should be furthered beyond the current task.
    /// </summary>
    public enum ContinuationConditionType
    {
        /// <summary>
        /// If any Management Agents have previously executed an import run profile and this has
        /// resulted in pending imports then the schedule should continue.
        /// </summary>
        ManagementAgentsHadImports
    }
}
using System.Collections.Generic;

namespace Tetron.Mim.SynchronisationScheduler.Models
{
    /// <summary>
    /// Represents an item of work for the scheduler to perform. Can contain child tasks.
    /// </summary>
    public class ScheduleTask
    {
        #region accessors
        /// <summary>
        /// A human-friendly name of the task which will appear in logs.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Defines the type of operation to be performed as part of the task.
        /// </summary>
        public ScheduleTaskType Type { get; set; }
        /// <summary>
        /// Defines what type of condition is to be evaluated for task types of ContinuationCondition.
        /// </summary>
        public ContinuationConditionType ConditionType { get; set; }
        /// <summary>
        /// Defines the operation to perform. 
        /// For Executable type tasks this is the full command-line including the path.
        /// For PowerShell type tasks this is just the path and script name.
        /// For ManagementAgent type tasks this is just the run profile name as seen in the Synchronisation Manager.
        /// For SqlServer type tasks this is the SQL command to execute, i.e. 'exec uspMySproc 1'
        /// </summary>
        public string Command { get; set; }
        /// <summary>
        /// For executable tasks - any arguments that need to accompany the command.
        /// </summary>
        public string Arguments { get; set; }
        /// <summary>
        /// For SqlServer type tasks this defines the server/host name and can optionally include the instance name as well, 
        /// i.e. "kclMimSqlSvr1" for the default instance on the kclMimSqlSvr1 host, or "kclMimSqlSvr1\Custom" for the 'Custom' instance on the same host.
        /// </summary>
        public string Server { get; set; }
        /// <summary>
        /// When applied to ManagementAgent tasks this ensures the task is executed only if there are pending changes in the management agent.
        /// </summary>
        public bool OnlyRunIfPendingExportsExist { get; set; }
        /// <summary>
        /// Additional tasks to be executed in parallel once this task has completed.
        /// </summary>
        public List<ScheduleTask> ChildTasks { get; set; }
        /// <summary>
        /// Some tasks may not complete for whatever reason and may be worth retrying, this identifies tasks which require retrying.
        /// </summary>
        public bool RetryRequired { get; set; }
        /// <summary>
        /// If set to true, any program window that would show will, otherwise it'll be hidden.
        /// The default is false.
        /// </summary>
        public bool ShowExecutableWindow { get; set; }
        #endregion

        #region constructors
        public ScheduleTask()
        {
            ChildTasks = new List<ScheduleTask>();
        }
        #endregion

        public new string ToString()
        {
            return Type == ScheduleTaskType.ManagementAgent ? $"{Name} - {Command}" : Name;
        }
    }
}
using System.Collections.Generic;

namespace Tetron.Mim.SynchronisationScheduler.Models
{
    /// <summary>
    /// Represents a whole schedule to be executed, including all tasks.
    /// </summary>
    public class Schedule
    {
        #region constructors
        public Schedule()
        {
            Tasks = new List<ScheduleTask>();
        }
        #endregion

        #region accessors
        /// <summary>
        /// The human-friendly name for the schedule.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// If set to true, on a task not completing, processing of the schedule will stop and no more tasks will be processed.
        /// </summary>
        public bool StopOnIncompletion { get; set; }
        /// <summary>
        /// The list of schedule tasks to be executed in parallel.
        /// </summary>
        public List<ScheduleTask> Tasks { get; set; }
        #endregion
    }
}
using Tetron.Mim.SynchronisationScheduler.Models;

namespace Tetron.Mim.SynchronisationScheduler.Interfaces
{
    /// <summary>
    /// Interface for executing schedules.
    /// </summary>
    public interface IScheduleExecutor
    {
        /// <summary>
        /// Executes a complete schedule.
        /// </summary>
        /// <param name="schedule">The schedule to execute.</param>
        void ExecuteSchedule(Schedule schedule);
    }
}

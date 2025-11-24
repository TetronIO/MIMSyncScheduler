using Serilog;
using System;
using Tetron.Mim.SynchronisationScheduler.Interfaces;
using Tetron.Mim.SynchronisationScheduler.Models;

namespace Tetron.Mim.SynchronisationScheduler.Services
{
    /// <summary>
    /// Executes complete schedules.
    /// </summary>
    public class ScheduleExecutor : IScheduleExecutor
    {
        private readonly ITaskExecutor _taskExecutor;
        private readonly string _loggingPrefix;

        public ScheduleExecutor(ITaskExecutor taskExecutor, bool whatIfMode = false)
        {
            _taskExecutor = taskExecutor ?? throw new ArgumentNullException(nameof(taskExecutor));
            _loggingPrefix = whatIfMode ? "WHATIF: " : string.Empty;
        }

        /// <inheritdoc />
        public void ExecuteSchedule(Schedule schedule)
        {
            var timer = new Timer();
            Log.Information($"{_loggingPrefix}Running schedule '{schedule.Name}'. Stopping on incomplete tasks: {schedule.StopOnIncompletion}");

            var managementAgentImportsHadChanges = false;
            _taskExecutor.ExecuteTasks(schedule.Tasks, schedule.StopOnIncompletion, ref managementAgentImportsHadChanges);

            timer.Stop();
        }
    }
}

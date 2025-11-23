using Serilog;
using System;

namespace Tetron.Mim.SynchronisationScheduler
{
    /// <summary>
    /// Times an operation. Useful for profiling how long methods or processes take to run and using that data to detect trends.
    /// Writes out the result to the log.
    /// </summary>
    /// <remarks>
    /// I did consider implementing IDisposable so you could wrap operations with a simple USING statement
    /// but this would be bad as if an exception was thrown during operation of the process being timed, then the timer would complete
    /// and a value written to the log that's actually not representative of the process. I think it's better to not log failed processes.
    /// </remarks>
    public class Timer
    {
        #region members
        private readonly DateTime _time;
        private readonly string _parentClassName;
        private readonly string _parentMethodName;
        #endregion

        #region constructors
        public Timer()
        {
            _parentClassName = Utilities.GetCallingClassName();
            _parentMethodName = Utilities.GetCallingMethodName();
            _time = DateTime.UtcNow;
        }
        #endregion

        /// <summary>
        /// Writes out the time-elapsed to the log and the debug console.
        /// </summary>
        public void Stop()
        {
            var et = DateTime.UtcNow - _time;
            Log.Debug($"{_parentClassName}.{_parentMethodName} took {et.Hours}hr {et.Minutes}m {et.Seconds}s {et.Milliseconds}ms / {et.Ticks} ticks to run.");
        }
    }
}

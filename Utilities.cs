using System.Diagnostics;

namespace Tetron.Mim.SynchronisationScheduler
{
    public static class Utilities
    {
        /// <summary>
        /// Retrieves the full namespace (including class) of the method that called the current one.
        /// </summary>
        internal static string GetCallingClassName()
        {
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(2);
            if (frame == null)
                return string.Empty;

            var method = frame.GetMethod();
            if (method == null)
                return string.Empty;

            var declaringType = method.DeclaringType;
            return declaringType != null ? declaringType.FullName : string.Empty;
        }

        /// <summary>
        /// Retrieves the name of the method that called the current one.
        /// </summary>
        internal static string GetCallingMethodName()
        {
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(2);
            if (frame == null)
                return string.Empty;

            var method = frame.GetMethod();
            return method == null ? string.Empty : method.Name;
        }

        /// <summary>
        /// To be used by any executable that needs to be called by the Synchronisation Scheduler and report its outcome.
        /// </summary>
        public enum SynchronisationTaskExitCode
        {
            Success = 0,
            Error = 1
        }
    }
}

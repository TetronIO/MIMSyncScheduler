namespace Tetron.Mim.SynchronisationScheduler.Interfaces
{
    /// <summary>
    /// Interface for executing external processes (executables, PowerShell, VBScript).
    /// Allows mocking of process execution for testing.
    /// </summary>
    public interface IProcessExecutor
    {
        /// <summary>
        /// Executes a PowerShell script.
        /// </summary>
        /// <param name="scriptPath">The full path to the PowerShell script.</param>
        /// <returns>True if the script executed successfully, false otherwise.</returns>
        bool ExecutePowerShellScript(string scriptPath);

        /// <summary>
        /// Executes a Visual Basic script.
        /// </summary>
        /// <param name="scriptPath">The full path to the VBScript file.</param>
        /// <returns>True if the script executed successfully, false otherwise.</returns>
        bool ExecuteVisualBasicScript(string scriptPath);

        /// <summary>
        /// Executes an external executable.
        /// </summary>
        /// <param name="executablePath">The path to the executable.</param>
        /// <param name="arguments">Optional arguments to pass to the executable.</param>
        /// <param name="showWindow">Whether to show the executable window.</param>
        /// <returns>True if the executable completed successfully, false otherwise.</returns>
        bool ExecuteExecutable(string executablePath, string arguments = null, bool showWindow = false);
    }
}

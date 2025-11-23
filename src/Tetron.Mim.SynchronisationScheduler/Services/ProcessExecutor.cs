using Serilog;
using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Tetron.Mim.SynchronisationScheduler.Interfaces;

namespace Tetron.Mim.SynchronisationScheduler.Services
{
    /// <summary>
    /// Executes external processes including PowerShell scripts, VBScripts, and executables.
    /// </summary>
    public class ProcessExecutor : IProcessExecutor
    {
        private readonly bool _whatIfMode;
        private readonly string _loggingPrefix;

        public ProcessExecutor(bool whatIfMode = false)
        {
            _whatIfMode = whatIfMode;
            _loggingPrefix = whatIfMode ? "WHATIF: " : string.Empty;
        }

        /// <inheritdoc />
        public bool ExecutePowerShellScript(string scriptPath)
        {
            if (_whatIfMode)
            {
                Log.Debug($"{_loggingPrefix}Executing PowerShell script: {scriptPath}");
                return true;
            }

            try
            {
                var timer = new Timer();
                var initialState = InitialSessionState.CreateDefault();
                initialState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Unrestricted;
                using var shell = PowerShell.Create(initialState);
                shell.Commands.AddScript(scriptPath);

                shell.Streams.Debug.DataAdded += PowerShellDebugStreamHandler;
                shell.Streams.Verbose.DataAdded += PowerShellVerboseStreamHandler;
                shell.Streams.Information.DataAdded += PowerShellInformationStreamHandler;
                shell.Streams.Warning.DataAdded += PowerShellWarningStreamHandler;
                shell.Streams.Error.DataAdded += PowerShellErrorStreamHandler;

                var results = shell.Invoke<string>();
                if (results == null || results.Count == 0)
                    return true;

                foreach (var result in results)
                    Log.Debug($"{_loggingPrefix}PowerShell output: {result}");

                timer.Stop();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{_loggingPrefix}Unhandled exception when executing PowerShell task: {scriptPath}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool ExecuteVisualBasicScript(string scriptPath)
        {
            if (_whatIfMode)
            {
                Log.Debug($"{_loggingPrefix}Executing Visual Basic script: {scriptPath}");
                return true;
            }

            try
            {
                var timer = new Timer();
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = "cscript",
                        Arguments = $"/Nologo \"{scriptPath}\"",
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    }
                };
                process.OutputDataReceived += VbsOutputDataReceivedHandler;
                process.ErrorDataReceived += VbsErrorDataReceivedHandler;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                timer.Stop();
                return (Utilities.SynchronisationTaskExitCode)process.ExitCode == Utilities.SynchronisationTaskExitCode.Success;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{_loggingPrefix}Unhandled exception when executing vbs task: {scriptPath}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool ExecuteExecutable(string executablePath, string arguments = null, bool showWindow = false)
        {
            if (_whatIfMode)
            {
                Log.Debug($"{_loggingPrefix}Executing executable: {executablePath}");
                return true;
            }

            try
            {
                var timer = new Timer();
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = executablePath,
                        Arguments = arguments ?? string.Empty,
                        CreateNoWindow = !showWindow,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    }
                };
                process.OutputDataReceived += ExecutableOutputDataReceivedHandler;
                process.ErrorDataReceived += ExecutableErrorDataReceivedHandler;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                timer.Stop();

                return (Utilities.SynchronisationTaskExitCode)process.ExitCode == Utilities.SynchronisationTaskExitCode.Success;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{_loggingPrefix}Unhandled exception when executing '{executablePath}'");
                return false;
            }
        }

        #region Event Handlers

        private void PowerShellVerboseStreamHandler(object sender, DataAddedEventArgs ea)
        {
            if (!(sender is PSDataCollection<VerboseRecord> streamObjectsReceived))
                return;

            var currentStreamRecord = streamObjectsReceived[ea.Index];
            Log.Verbose($"{_loggingPrefix}PowerShell: {currentStreamRecord.Message}");
        }

        private void PowerShellDebugStreamHandler(object sender, DataAddedEventArgs ea)
        {
            if (!(sender is PSDataCollection<DebugRecord> streamObjectsReceived))
                return;

            var currentStreamRecord = streamObjectsReceived[ea.Index];
            Log.Debug($"{_loggingPrefix}PowerShell: {currentStreamRecord.Message}");
        }

        private void PowerShellInformationStreamHandler(object sender, DataAddedEventArgs ea)
        {
            if (!(sender is PSDataCollection<InformationRecord> streamObjectsReceived))
                return;

            var currentStreamRecord = streamObjectsReceived[ea.Index];
            Log.Information($"{_loggingPrefix}PowerShell: {currentStreamRecord.MessageData}");
        }

        private void PowerShellWarningStreamHandler(object sender, DataAddedEventArgs ea)
        {
            if (!(sender is PSDataCollection<WarningRecord> streamObjectsReceived))
                return;

            var currentStreamRecord = streamObjectsReceived[ea.Index];
            Log.Warning($"{_loggingPrefix}PowerShell: {currentStreamRecord.Message}");
        }

        private void PowerShellErrorStreamHandler(object sender, DataAddedEventArgs ea)
        {
            if (!(sender is PSDataCollection<ErrorRecord> streamObjectsReceived))
                return;

            var currentStreamRecord = streamObjectsReceived[ea.Index];
            var errorMessage = currentStreamRecord.Exception != null
                ? currentStreamRecord.Exception.Message
                : currentStreamRecord.ToString();
            Log.Error($"{_loggingPrefix}PowerShell: {errorMessage}");
        }

        private void VbsErrorDataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            if (e != null && !string.IsNullOrEmpty(e.Data))
                Log.Error($"{_loggingPrefix}VBS: {e.Data}");
        }

        private void VbsOutputDataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e?.Data))
                Log.Debug($"{_loggingPrefix}VBS: {e.Data}");
        }

        private void ExecutableErrorDataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            if (e != null && !string.IsNullOrEmpty(e.Data))
                Log.Error($"{_loggingPrefix}Executable: {e.Data}");
        }

        private void ExecutableOutputDataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e?.Data))
                Log.Debug($"{_loggingPrefix}Executable: {e.Data}");
        }

        #endregion
    }
}

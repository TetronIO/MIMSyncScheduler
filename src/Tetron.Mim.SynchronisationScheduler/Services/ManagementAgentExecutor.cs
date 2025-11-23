using Serilog;
using System;
using System.Management;
using Tetron.Mim.SynchronisationScheduler.Interfaces;

namespace Tetron.Mim.SynchronisationScheduler.Services
{
    /// <summary>
    /// Executes MIM Management Agent operations via WMI.
    /// </summary>
    public class ManagementAgentExecutor : IManagementAgentExecutor
    {
        private readonly bool _whatIfMode;

        public ManagementAgentExecutor(bool whatIfMode = false)
        {
            _whatIfMode = whatIfMode;
        }

        /// <inheritdoc />
        public bool ExecuteRunProfile(string managementAgentName, string runProfileName, out bool retryRequired)
        {
            retryRequired = false;

            if (_whatIfMode)
            {
                Log.Debug($"WHATIF: Executing run profile: {managementAgentName}\\{runProfileName}");
                return true;
            }

            try
            {
                var timer = new Timer();
                const string mimSyncServiceMaObjectSpace = "MIIS_ManagementAgent.Name";
                const string mimSyncServiceWmiNameSpace = "root\\MicrosoftIdentityIntegrationServer";

                var managementObject = new ManagementObject(
                    mimSyncServiceWmiNameSpace,
                    $"{mimSyncServiceMaObjectSpace}='{managementAgentName}'",
                    null);

                var inParameters = managementObject.GetMethodParameters("Execute");
                inParameters["RunProfileName"] = runProfileName;
                var result = managementObject.InvokeMethod("Execute", inParameters, null);

                if (result != null)
                {
                    foreach (var property in result.Properties)
                        Log.Information($"MA: {managementAgentName}, run profile: {runProfileName}, result property: {property.Name}, value: {property.Value}");
                }

                var returnValue = result != null ? result.Properties["ReturnValue"].Value.ToString() : "stopped";
                Log.Information($"MA: {managementAgentName}, Run profile: {runProfileName}, ReturnValue: {returnValue}");

                var goodResponse = !(returnValue.StartsWith("stopped") ||
                                     returnValue.StartsWith("call-failure:") ||
                                     returnValue.StartsWith("no-start-") ||
                                     returnValue.Equals("sql-deadlock"));

                if (returnValue.Equals("stopped-user-termination-from-wmi-or-ui") || returnValue.Equals("stopped-object-limit"))
                    goodResponse = true;

                if (returnValue.Equals("sql-deadlock"))
                {
                    retryRequired = true;
                    return false;
                }

                timer.Stop();
                return goodResponse;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unhandled exception ({managementAgentName}\\{runProfileName}): {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool HasPendingExports(string managementAgentName)
        {
            if (_whatIfMode)
            {
                Log.Debug($"WHATIF: Checking pending exports for: {managementAgentName}");
                return false;
            }

            try
            {
                const string mimSyncServiceMaObjectSpace = "MIIS_ManagementAgent.Name";
                const string mimSyncServiceWmiNameSpace = "root\\MicrosoftIdentityIntegrationServer";

                var managementObject = new ManagementObject(
                    mimSyncServiceWmiNameSpace,
                    $"{mimSyncServiceMaObjectSpace}='{managementAgentName}'",
                    null);

                managementObject.Get();
                var numExportAdd = int.Parse(managementObject.Properties["NumExportAdd"].Value.ToString());
                var numExportUpdate = int.Parse(managementObject.Properties["NumExportUpdate"].Value.ToString());
                var numExportDelete = int.Parse(managementObject.Properties["NumExportDelete"].Value.ToString());

                return numExportAdd > 0 || numExportUpdate > 0 || numExportDelete > 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unhandled exception checking pending exports for {managementAgentName}: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool HasPendingImports(string managementAgentName)
        {
            if (_whatIfMode)
            {
                Log.Debug($"WHATIF: Checking pending imports for: {managementAgentName}");
                return false;
            }

            try
            {
                const string mimSyncServiceMaObjectSpace = "MIIS_ManagementAgent.Name";
                const string mimSyncServiceWmiNameSpace = "root\\MicrosoftIdentityIntegrationServer";

                var managementObject = new ManagementObject(
                    mimSyncServiceWmiNameSpace,
                    $"{mimSyncServiceMaObjectSpace}='{managementAgentName}'",
                    null);

                managementObject.Get();
                var numImportAdd = int.Parse(managementObject.Properties["NumImportAdd"].Value.ToString());
                var numImportUpdate = int.Parse(managementObject.Properties["NumImportUpdate"].Value.ToString());
                var numImportDelete = int.Parse(managementObject.Properties["NumImportDelete"].Value.ToString());

                return numImportAdd > 0 || numImportUpdate > 0 || numImportDelete > 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unhandled exception checking pending imports for {managementAgentName}: {ex.Message}");
                return false;
            }
        }
    }
}

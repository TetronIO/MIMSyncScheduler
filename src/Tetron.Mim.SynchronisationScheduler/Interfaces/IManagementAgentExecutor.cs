namespace Tetron.Mim.SynchronisationScheduler.Interfaces
{
    /// <summary>
    /// Interface for executing MIM Management Agent operations.
    /// Allows mocking of MIM operations for testing.
    /// </summary>
    public interface IManagementAgentExecutor
    {
        /// <summary>
        /// Executes a run profile against a management agent.
        /// </summary>
        /// <param name="managementAgentName">The name of the management agent.</param>
        /// <param name="runProfileName">The name of the run profile to execute.</param>
        /// <param name="retryRequired">Output parameter indicating if the operation should be retried.</param>
        /// <returns>True if the run profile completed successfully, false otherwise.</returns>
        bool ExecuteRunProfile(string managementAgentName, string runProfileName, out bool retryRequired);

        /// <summary>
        /// Checks if there are pending exports in a management agent.
        /// </summary>
        /// <param name="managementAgentName">The name of the management agent to check.</param>
        /// <returns>True if there are pending exports, false otherwise.</returns>
        bool HasPendingExports(string managementAgentName);

        /// <summary>
        /// Checks if there are pending imports in a management agent.
        /// </summary>
        /// <param name="managementAgentName">The name of the management agent to check.</param>
        /// <returns>True if there are pending imports, false otherwise.</returns>
        bool HasPendingImports(string managementAgentName);
    }
}

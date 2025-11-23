namespace Tetron.Mim.SynchronisationScheduler.Interfaces
{
    /// <summary>
    /// Interface for executing SQL commands.
    /// Allows mocking of SQL Server operations for testing.
    /// </summary>
    public interface ISqlExecutor
    {
        /// <summary>
        /// Executes a SQL command on the specified server.
        /// </summary>
        /// <param name="command">The SQL command to execute.</param>
        /// <param name="server">The server name/instance.</param>
        /// <returns>True if the command executed successfully, false otherwise.</returns>
        bool ExecuteCommand(string command, string server);
    }
}

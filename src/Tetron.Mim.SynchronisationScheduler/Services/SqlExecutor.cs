using Serilog;
using System;
using System.Data.SqlClient;
using Tetron.Mim.SynchronisationScheduler.Interfaces;

namespace Tetron.Mim.SynchronisationScheduler.Services
{
    /// <summary>
    /// Executes SQL commands against SQL Server.
    /// </summary>
    public class SqlExecutor : ISqlExecutor
    {
        private readonly bool _whatIfMode;

        public SqlExecutor(bool whatIfMode = false)
        {
            _whatIfMode = whatIfMode;
        }

        /// <inheritdoc />
        public bool ExecuteCommand(string command, string server)
        {
            if (_whatIfMode)
            {
                Log.Debug($"WHATIF: Executing SQL command on server: {server}");
                return true;
            }

            try
            {
                var timer = new Timer();
                var connectionString = $"Server={server};Integrated Security=true;";
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                using var sqlCommand = new SqlCommand(command, connection);
                sqlCommand.ExecuteNonQuery();

                timer.Stop();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unhandled exception when executing SQL task on server: {server}, command: {command}");
                return false;
            }
        }
    }
}

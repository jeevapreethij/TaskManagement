using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace TaskManagement.Data
{
    public interface IDbConnectionFactory
    {
        Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
    }

    public sealed class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("A database connection string is required.", nameof(connectionString));
            }

            _connectionString = connectionString;
        }

        public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }
    }
}

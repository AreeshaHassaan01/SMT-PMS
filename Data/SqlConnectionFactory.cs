using System.Data;
using Microsoft.Data.SqlClient;

namespace PMS.Data
{
    public interface ISqlConnectionFactory
    {
        SqlConnection CreateConnection();
        string GetConnectionString();
    }

    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public string GetConnectionString() => _connectionString;
    }
}


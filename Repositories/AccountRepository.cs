using System.Data;
using Microsoft.Data.SqlClient;
using PMS.Data;
using PMS.Models;

namespace PMS.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<AccountRepository> _logger;

        public AccountRepository(ISqlConnectionFactory connectionFactory, ILogger<AccountRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<UserAccount?> GetByUsernameAsync(string username)
        {
            var sql = @"
                SELECT Id, Username, PasswordHash, FullName, Email, Role, CreatedAt, LastLoginAt
                FROM UserAccounts
                WHERE LOWER(Username) = LOWER(@Username);";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = username.Trim();

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapAccount(reader);
            }

            return null;
        }

        public async Task<UserAccount?> GetByEmailOrUsernameAsync(string identifier)
        {
            var sql = @"
                SELECT Id, Username, PasswordHash, FullName, Email, Role, CreatedAt, LastLoginAt
                FROM UserAccounts
                WHERE LOWER(Email) = LOWER(@Identifier) OR LOWER(Username) = LOWER(@Identifier);";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Identifier", SqlDbType.NVarChar, 150).Value = identifier.Trim();

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapAccount(reader);
            }

            return null;
        }

        public async Task<UserAccount?> GetByIdAsync(int id)
        {
            var sql = @"
                SELECT Id, Username, PasswordHash, FullName, Email, Role, CreatedAt, LastLoginAt
                FROM UserAccounts
                WHERE Id = @Id;";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapAccount(reader);
            }

            return null;
        }

        public async Task<int> CreateAsync(UserAccount account)
        {
            var sql = @"
                INSERT INTO UserAccounts (Username, PasswordHash, FullName, Email, Role, CreatedAt)
                OUTPUT INSERTED.Id
                VALUES (@Username, @PasswordHash, @FullName, @Email, @Role, GETUTCDATE());";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = account.Username.Trim();
            cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 256).Value = account.PasswordHash;
            cmd.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value = account.FullName.Trim();
            cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = account.Email.Trim();
            cmd.Parameters.Add("@Role", SqlDbType.NVarChar, 50).Value = string.IsNullOrEmpty(account.Role) ? "Customer" : account.Role;

            await conn.OpenAsync();
            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            account.Id = newId;
            return newId;
        }

        public async Task<bool> UpdateAsync(UserAccount account)
        {
            var sql = @"
                UPDATE UserAccounts
                SET FullName = @FullName,
                    Email = @Email,
                    Role = @Role
                WHERE Id = @Id;";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = account.Id;
            cmd.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value = account.FullName.Trim();
            cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = account.Email.Trim();
            cmd.Parameters.Add("@Role", SqlDbType.NVarChar, 50).Value = account.Role;

            await conn.OpenAsync();
            var affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }

        public async Task<bool> UpdateRoleAsync(int id, string role)
        {
            var sql = "UPDATE UserAccounts SET Role = @Role WHERE Id = @Id;";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            cmd.Parameters.Add("@Role", SqlDbType.NVarChar, 50).Value = role;

            await conn.OpenAsync();
            var affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }

        public async Task<bool> UpdatePasswordAsync(int id, string passwordHash)
        {
            var sql = "UPDATE UserAccounts SET PasswordHash = @PasswordHash WHERE Id = @Id;";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 256).Value = passwordHash;

            await conn.OpenAsync();
            var affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }

        public async Task<bool> UpdateLastLoginAsync(int id)
        {
            var sql = "UPDATE UserAccounts SET LastLoginAt = GETUTCDATE() WHERE Id = @Id;";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await conn.OpenAsync();
            var affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }

        public async Task<IEnumerable<UserAccount>> GetAllAsync()
        {
            var list = new List<UserAccount>();
            var sql = @"SELECT Id, Username, PasswordHash, FullName, Email, Role, CreatedAt, LastLoginAt FROM UserAccounts ORDER BY Id;";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapAccount(reader));
            }

            return list;
        }

        private static UserAccount MapAccount(SqlDataReader reader)
        {
            return new UserAccount
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Role = reader.GetString(reader.GetOrdinal("Role")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                LastLoginAt = reader.IsDBNull(reader.GetOrdinal("LastLoginAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LastLoginAt"))
            };
        }
    }
}

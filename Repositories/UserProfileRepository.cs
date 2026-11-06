using Microsoft.Data.SqlClient;
using PMS.Data;
using PMS.Models;

namespace PMS.Repositories
{
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public UserProfileRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<UserProfile?> GetByUserIdAsync(int userId)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT Id, UserId, FullName, Phone, Address, City, Province, PostalCode, Country, CreatedAt, UpdatedAt
                FROM UserProfiles WHERE UserId = @UserId";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;
            return MapProfile(reader);
        }

        public async Task UpsertAsync(UserProfile profile)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                IF EXISTS (SELECT 1 FROM UserProfiles WHERE UserId = @UserId)
                    UPDATE UserProfiles
                    SET FullName = @FullName, Phone = @Phone, Address = @Address,
                        City = @City, Province = @Province, PostalCode = @PostalCode,
                        Country = @Country, UpdatedAt = GETUTCDATE()
                    WHERE UserId = @UserId
                ELSE
                    INSERT INTO UserProfiles (UserId, FullName, Phone, Address, City, Province, PostalCode, Country, CreatedAt)
                    VALUES (@UserId, @FullName, @Phone, @Address, @City, @Province, @PostalCode, @Country, GETUTCDATE())";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", profile.UserId);
            cmd.Parameters.AddWithValue("@FullName", (object?)profile.FullName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Phone", (object?)profile.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", (object?)profile.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@City", (object?)profile.City ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Province", (object?)profile.Province ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PostalCode", (object?)profile.PostalCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Country", string.IsNullOrWhiteSpace(profile.Country) ? "Pakistan" : profile.Country);
            await cmd.ExecuteNonQueryAsync();
        }

        private static UserProfile MapProfile(SqlDataReader r) => new()
        {
            Id         = r.GetInt32(r.GetOrdinal("Id")),
            UserId     = r.GetInt32(r.GetOrdinal("UserId")),
            FullName   = r.IsDBNull(r.GetOrdinal("FullName")) ? string.Empty : r.GetString(r.GetOrdinal("FullName")),
            Phone      = r.IsDBNull(r.GetOrdinal("Phone")) ? null : r.GetString(r.GetOrdinal("Phone")),
            Address    = r.IsDBNull(r.GetOrdinal("Address")) ? null : r.GetString(r.GetOrdinal("Address")),
            City       = r.IsDBNull(r.GetOrdinal("City")) ? null : r.GetString(r.GetOrdinal("City")),
            Province   = r.IsDBNull(r.GetOrdinal("Province")) ? null : r.GetString(r.GetOrdinal("Province")),
            PostalCode = r.IsDBNull(r.GetOrdinal("PostalCode")) ? null : r.GetString(r.GetOrdinal("PostalCode")),
            Country    = r.IsDBNull(r.GetOrdinal("Country")) ? "Pakistan" : r.GetString(r.GetOrdinal("Country")),
            CreatedAt  = r.GetDateTime(r.GetOrdinal("CreatedAt")),
            UpdatedAt  = r.IsDBNull(r.GetOrdinal("UpdatedAt")) ? null : r.GetDateTime(r.GetOrdinal("UpdatedAt")),
        };
    }
}


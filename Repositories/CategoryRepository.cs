using System.Data;
using Microsoft.Data.SqlClient;
using PMS.Data;
using PMS.Models;

namespace PMS.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<CategoryRepository> _logger;

        public CategoryRepository(ISqlConnectionFactory connectionFactory, ILogger<CategoryRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            var categories = new List<Category>();
            var sql = @"
                SELECT c.Id, c.Name, c.Description, c.DisplayOrder,
                       COUNT(p.Id) AS ProductCount
                FROM Categories c
                LEFT JOIN Products p ON c.Id = p.CategoryId
                GROUP BY c.Id, c.Name, c.Description, c.DisplayOrder
                ORDER BY c.DisplayOrder, c.Name;";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
                    ProductCount = reader.GetInt32(reader.GetOrdinal("ProductCount"))
                });
            }

            return categories;
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            var sql = @"
                SELECT c.Id, c.Name, c.Description, c.DisplayOrder,
                       COUNT(p.Id) AS ProductCount
                FROM Categories c
                LEFT JOIN Products p ON c.Id = p.CategoryId
                WHERE c.Id = @Id
                GROUP BY c.Id, c.Name, c.Description, c.DisplayOrder;";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Category
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
                    ProductCount = reader.GetInt32(reader.GetOrdinal("ProductCount"))
                };
            }

            return null;
        }

        public async Task<int> CreateAsync(Category category)
        {
            var sql = @"
                INSERT INTO Categories (Name, Description, DisplayOrder)
                OUTPUT INSERTED.Id
                VALUES (@Name, @Description, @DisplayOrder);";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = category.Name;
            cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = (object?)category.Description ?? DBNull.Value;
            cmd.Parameters.Add("@DisplayOrder", SqlDbType.Int).Value = category.DisplayOrder;

            await conn.OpenAsync();
            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            category.Id = newId;
            return newId;
        }

        public async Task<bool> UpdateAsync(Category category)
        {
            var sql = @"
                UPDATE Categories
                SET Name = @Name,
                    Description = @Description,
                    DisplayOrder = @DisplayOrder
                WHERE Id = @Id;";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = category.Id;
            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = category.Name;
            cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = (object?)category.Description ?? DBNull.Value;
            cmd.Parameters.Add("@DisplayOrder", SqlDbType.Int).Value = category.DisplayOrder;

            await conn.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var sql = "DELETE FROM Categories WHERE Id = @Id;";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await conn.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}

using System.Data;
using Microsoft.Data.SqlClient;
using PMS.Data;
using PMS.Models;

namespace PMS.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(ISqlConnectionFactory connectionFactory, ILogger<ProductRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<Product>> GetAllAsync(string? searchTerm = null, int? categoryId = null)
        {
            var products = new List<Product>();

            var sql = @"
                SELECT p.Id, p.Name, p.CategoryId, c.Name AS CategoryName, 
                       p.Price, p.Stock, p.Description, p.ImageUrl, p.CreatedAt, p.UpdatedAt
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.Id
                WHERE 1 = 1";

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += " AND (p.Name LIKE @SearchTerm OR p.Description LIKE @SearchTerm)";
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                sql += " AND p.CategoryId = @CategoryId";
            }

            sql += " ORDER BY p.Id ASC";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                cmd.Parameters.Add("@SearchTerm", SqlDbType.NVarChar, 150).Value = $"%{searchTerm.Trim()}%";
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                cmd.Parameters.Add("@CategoryId", SqlDbType.Int).Value = categoryId.Value;
            }

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(MapProduct(reader));
            }

            return products;
        }

        public async Task<IEnumerable<Product>> GetRecentProductsAsync(int count = 5)
        {
            var products = new List<Product>();
            var sql = $@"
                SELECT TOP ({count}) p.Id, p.Name, p.CategoryId, c.Name AS CategoryName, 
                       p.Price, p.Stock, p.Description, p.ImageUrl, p.CreatedAt, p.UpdatedAt
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.Id
                ORDER BY p.Id ASC";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(MapProduct(reader));
            }

            return products;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            var sql = @"
                SELECT p.Id, p.Name, p.CategoryId, c.Name AS CategoryName, 
                       p.Price, p.Stock, p.Description, p.ImageUrl, p.CreatedAt, p.UpdatedAt
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.Id
                WHERE p.Id = @Id";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapProduct(reader);
            }

            return null;
        }

        public async Task<int> CreateAsync(Product product)
        {
            var sql = @"
                INSERT INTO Products (Name, CategoryId, Price, Stock, Description, ImageUrl, CreatedAt)
                OUTPUT INSERTED.Id
                VALUES (@Name, @CategoryId, @Price, @Stock, @Description, @ImageUrl, GETUTCDATE());";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = product.Name;
            cmd.Parameters.Add("@CategoryId", SqlDbType.Int).Value = product.CategoryId;
            cmd.Parameters.Add("@Price", SqlDbType.Decimal).Value = product.Price;
            cmd.Parameters.Add("@Stock", SqlDbType.Int).Value = product.Stock;
            cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 2000).Value = (object?)product.Description ?? DBNull.Value;
            cmd.Parameters.Add("@ImageUrl", SqlDbType.NVarChar, 500).Value = (object?)product.ImageUrl ?? DBNull.Value;

            await conn.OpenAsync();
            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            product.Id = newId;
            return newId;
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            var sql = @"
                UPDATE Products
                SET Name = @Name,
                    CategoryId = @CategoryId,
                    Price = @Price,
                    Stock = @Stock,
                    Description = @Description,
                    ImageUrl = CASE WHEN @ImageUrl IS NOT NULL THEN @ImageUrl ELSE ImageUrl END,
                    UpdatedAt = GETUTCDATE()
                WHERE Id = @Id;";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = product.Id;
            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = product.Name;
            cmd.Parameters.Add("@CategoryId", SqlDbType.Int).Value = product.CategoryId;
            cmd.Parameters.Add("@Price", SqlDbType.Decimal).Value = product.Price;
            cmd.Parameters.Add("@Stock", SqlDbType.Int).Value = product.Stock;
            cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 2000).Value = (object?)product.Description ?? DBNull.Value;
            cmd.Parameters.Add("@ImageUrl", SqlDbType.NVarChar, 500).Value = (object?)product.ImageUrl ?? DBNull.Value;

            await conn.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var sql = "DELETE FROM Products WHERE Id = @Id;";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await conn.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var sql = @"
                SELECT 
                    COUNT(1) AS TotalProducts,
                    (SELECT COUNT(1) FROM Categories) AS TotalCategories,
                    ISNULL(SUM(CASE WHEN Stock > 5 THEN 1 ELSE 0 END), 0) AS InStock,
                    ISNULL(SUM(CASE WHEN Stock > 0 AND Stock <= 5 THEN 1 ELSE 0 END), 0) AS LowStock,
                    ISNULL(SUM(CASE WHEN Stock = 0 THEN 1 ELSE 0 END), 0) AS OutOfStock
                FROM Products;";

            using var conn = _connectionFactory.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new DashboardStats
                {
                    TotalProducts = reader.GetInt32(reader.GetOrdinal("TotalProducts")),
                    TotalCategories = reader.GetInt32(reader.GetOrdinal("TotalCategories")),
                    InStock = reader.GetInt32(reader.GetOrdinal("InStock")),
                    LowStock = reader.GetInt32(reader.GetOrdinal("LowStock")),
                    OutOfStock = reader.GetInt32(reader.GetOrdinal("OutOfStock"))
                };
            }

            return new DashboardStats();
        }

        private static Product MapProduct(SqlDataReader reader)
        {
            return new Product
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? null : reader.GetString(reader.GetOrdinal("CategoryName")),
                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                Stock = reader.GetInt32(reader.GetOrdinal("Stock")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader.GetString(reader.GetOrdinal("ImageUrl")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }
    }
}

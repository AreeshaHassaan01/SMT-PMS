using Microsoft.Data.SqlClient;
using PMS.Data;
using PMS.Models;

namespace PMS.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public CartRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<CartItem>> GetCartByUserIdAsync(int userId)
        {
            var items = new List<CartItem>();
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT ci.Id, ci.UserId, ci.ProductId, ci.Quantity, ci.AddedAt,
                       p.Name AS ProductName, p.Price AS UnitPrice, p.ImageUrl AS ProductImageUrl,
                       c.Name AS CategoryName
                FROM CartItems ci
                INNER JOIN Products p ON ci.ProductId = p.Id
                LEFT JOIN Categories c ON p.CategoryId = c.Id
                WHERE ci.UserId = @UserId
                ORDER BY ci.AddedAt DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new CartItem
                {
                    Id              = reader.GetInt32(reader.GetOrdinal("Id")),
                    UserId          = reader.GetInt32(reader.GetOrdinal("UserId")),
                    ProductId       = reader.GetInt32(reader.GetOrdinal("ProductId")),
                    Quantity        = reader.GetInt32(reader.GetOrdinal("Quantity")),
                    AddedAt         = reader.GetDateTime(reader.GetOrdinal("AddedAt")),
                    ProductName     = reader.GetString(reader.GetOrdinal("ProductName")),
                    UnitPrice       = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                    ProductImageUrl = reader.IsDBNull(reader.GetOrdinal("ProductImageUrl")) ? null : reader.GetString(reader.GetOrdinal("ProductImageUrl")),
                    CategoryName    = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? null : reader.GetString(reader.GetOrdinal("CategoryName")),
                });
            }
            return items;
        }

        public async Task<int> GetCartItemCountAsync(int userId)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("SELECT ISNULL(SUM(Quantity), 0) FROM CartItems WHERE UserId = @UserId", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task AddToCartAsync(int userId, int productId, int quantity)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            // If item already in cart, increment quantity
            const string sql = @"
                IF EXISTS (SELECT 1 FROM CartItems WHERE UserId = @UserId AND ProductId = @ProductId)
                    UPDATE CartItems SET Quantity = Quantity + @Quantity WHERE UserId = @UserId AND ProductId = @ProductId
                ELSE
                    INSERT INTO CartItems (UserId, ProductId, Quantity, AddedAt)
                    VALUES (@UserId, @ProductId, @Quantity, GETUTCDATE())";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@ProductId", productId);
            cmd.Parameters.AddWithValue("@Quantity", quantity);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateQuantityAsync(int cartItemId, int userId, int quantity)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                "UPDATE CartItems SET Quantity = @Quantity WHERE Id = @Id AND UserId = @UserId", conn);
            cmd.Parameters.AddWithValue("@Quantity", quantity);
            cmd.Parameters.AddWithValue("@Id", cartItemId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task RemoveFromCartAsync(int cartItemId, int userId)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                "DELETE FROM CartItems WHERE Id = @Id AND UserId = @UserId", conn);
            cmd.Parameters.AddWithValue("@Id", cartItemId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ClearCartAsync(int userId)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("DELETE FROM CartItems WHERE UserId = @UserId", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}


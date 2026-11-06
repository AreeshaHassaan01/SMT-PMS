using Microsoft.Data.SqlClient;
using PMS.Data;
using PMS.Models;

namespace PMS.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public OrderRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CreateOrderAsync(Order order, IEnumerable<CartItem> cartItems)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Insert Order
                const string orderSql = @"
                    INSERT INTO Orders (UserId, TotalAmount, Status, PaymentMethod, ShippingAddress, CreatedAt)
                    OUTPUT INSERTED.Id
                    VALUES (@UserId, @TotalAmount, @Status, @PaymentMethod, @ShippingAddress, GETUTCDATE())";

                int orderId;
                using (var cmd = new SqlCommand(orderSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@UserId", order.UserId);
                    cmd.Parameters.AddWithValue("@TotalAmount", order.TotalAmount);
                    cmd.Parameters.AddWithValue("@Status", "Pending");
                    cmd.Parameters.AddWithValue("@PaymentMethod", order.PaymentMethod);
                    cmd.Parameters.AddWithValue("@ShippingAddress", (object?)order.ShippingAddress ?? DBNull.Value);
                    orderId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Insert order items
                const string itemSql = @"
                    INSERT INTO OrderItems (OrderId, ProductId, ProductName, ProductImageUrl, UnitPrice, Quantity)
                    VALUES (@OrderId, @ProductId, @ProductName, @ProductImageUrl, @UnitPrice, @Quantity)";

                foreach (var item in cartItems)
                {
                    using var itemCmd = new SqlCommand(itemSql, conn, transaction);
                    itemCmd.Parameters.AddWithValue("@OrderId", orderId);
                    itemCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    itemCmd.Parameters.AddWithValue("@ProductName", item.ProductName);
                    itemCmd.Parameters.AddWithValue("@ProductImageUrl", (object?)item.ProductImageUrl ?? DBNull.Value);
                    itemCmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                    itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    await itemCmd.ExecuteNonQueryAsync();

                    // Reduce stock
                    using var stockCmd = new SqlCommand(
                        "UPDATE Products SET Stock = CASE WHEN Stock >= @Qty THEN Stock - @Qty ELSE 0 END WHERE Id = @ProductId",
                        conn, transaction);
                    stockCmd.Parameters.AddWithValue("@Qty", item.Quantity);
                    stockCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    await stockCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return orderId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId)
        {
            var orders = new List<Order>();
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT o.Id, o.UserId, o.TotalAmount, o.Status, o.PaymentMethod, o.ShippingAddress,
                       o.CreatedAt, o.DeliveredAt
                FROM Orders o
                WHERE o.UserId = @UserId
                ORDER BY o.CreatedAt DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(MapOrder(reader));
            }
            reader.Close();

            // Load items for each order
            foreach (var order in orders)
            {
                order.Items = (await GetOrderItemsAsync(order.Id, conn)).ToList();
            }
            return orders;
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId, int userId)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT o.Id, o.UserId, o.TotalAmount, o.Status, o.PaymentMethod, o.ShippingAddress,
                       o.CreatedAt, o.DeliveredAt
                FROM Orders o
                WHERE o.Id = @OrderId AND o.UserId = @UserId";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@OrderId", orderId);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var order = MapOrder(reader);
            reader.Close();

            order.Items = (await GetOrderItemsAsync(order.Id, conn)).ToList();
            return order;
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            var orders = new List<Order>();
            using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT o.Id, o.UserId, o.TotalAmount, o.Status, o.PaymentMethod, o.ShippingAddress,
                       o.CreatedAt, o.DeliveredAt
                FROM Orders o
                ORDER BY o.CreatedAt DESC";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(MapOrder(reader));
            }
            reader.Close();

            foreach (var order in orders)
            {
                order.Items = (await GetOrderItemsAsync(order.Id, conn)).ToList();
            }
            return orders;
        }

        private static Order MapOrder(SqlDataReader r) => new()
        {
            Id              = r.GetInt32(r.GetOrdinal("Id")),
            UserId          = r.GetInt32(r.GetOrdinal("UserId")),
            TotalAmount     = r.GetDecimal(r.GetOrdinal("TotalAmount")),
            Status          = r.GetString(r.GetOrdinal("Status")),
            PaymentMethod   = r.GetString(r.GetOrdinal("PaymentMethod")),
            ShippingAddress = r.IsDBNull(r.GetOrdinal("ShippingAddress")) ? null : r.GetString(r.GetOrdinal("ShippingAddress")),
            CreatedAt       = r.GetDateTime(r.GetOrdinal("CreatedAt")),
            DeliveredAt     = r.IsDBNull(r.GetOrdinal("DeliveredAt")) ? null : r.GetDateTime(r.GetOrdinal("DeliveredAt")),
        };

        private static async Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId, SqlConnection conn)
        {
            var items = new List<OrderItem>();
            const string sql = @"
                SELECT Id, OrderId, ProductId, ProductName, ProductImageUrl, UnitPrice, Quantity
                FROM OrderItems WHERE OrderId = @OrderId";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@OrderId", orderId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new OrderItem
                {
                    Id              = reader.GetInt32(reader.GetOrdinal("Id")),
                    OrderId         = reader.GetInt32(reader.GetOrdinal("OrderId")),
                    ProductId       = reader.GetInt32(reader.GetOrdinal("ProductId")),
                    ProductName     = reader.GetString(reader.GetOrdinal("ProductName")),
                    ProductImageUrl = reader.IsDBNull(reader.GetOrdinal("ProductImageUrl")) ? null : reader.GetString(reader.GetOrdinal("ProductImageUrl")),
                    UnitPrice       = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                    Quantity        = reader.GetInt32(reader.GetOrdinal("Quantity")),
                });
            }
            return items;
        }
    }
}


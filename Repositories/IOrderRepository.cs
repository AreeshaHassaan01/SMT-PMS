using PMS.Models;

namespace PMS.Repositories
{
    public interface IOrderRepository
    {
        Task<int> CreateOrderAsync(Order order, IEnumerable<CartItem> cartItems);
        Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId);
        Task<Order?> GetOrderByIdAsync(int orderId, int userId);
        Task<IEnumerable<Order>> GetAllOrdersAsync();
    }
}


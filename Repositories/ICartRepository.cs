using PMS.Models;

namespace PMS.Repositories
{
    public interface ICartRepository
    {
        Task<IEnumerable<CartItem>> GetCartByUserIdAsync(int userId);
        Task<int> GetCartItemCountAsync(int userId);
        Task AddToCartAsync(int userId, int productId, int quantity);
        Task UpdateQuantityAsync(int cartItemId, int userId, int quantity);
        Task RemoveFromCartAsync(int cartItemId, int userId);
        Task ClearCartAsync(int userId);
    }
}


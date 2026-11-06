using PMS.Models;

namespace PMS.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync(string? searchTerm = null, int? categoryId = null);
        Task<Product?> GetByIdAsync(int id);
        Task<int> CreateAsync(Product product);
        Task<bool> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
        Task<DashboardStats> GetDashboardStatsAsync();
        Task<IEnumerable<Product>> GetRecentProductsAsync(int count = 5);
    }
}


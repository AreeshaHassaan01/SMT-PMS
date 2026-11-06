using PMS.Models;

namespace PMS.Repositories
{
    public interface IAccountRepository
    {
        Task<UserAccount?> GetByUsernameAsync(string username);
        Task<UserAccount?> GetByEmailOrUsernameAsync(string identifier);
        Task<UserAccount?> GetByIdAsync(int id);
        Task<int> CreateAsync(UserAccount account);
        Task<bool> UpdateAsync(UserAccount account);
        Task<bool> UpdateRoleAsync(int id, string role);
        Task<bool> UpdatePasswordAsync(int id, string passwordHash);
        Task<bool> UpdateLastLoginAsync(int id);
        Task<IEnumerable<UserAccount>> GetAllAsync();
    }
}

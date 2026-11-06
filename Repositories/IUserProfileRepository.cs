using PMS.Models;

namespace PMS.Repositories
{
    public interface IUserProfileRepository
    {
        Task<UserProfile?> GetByUserIdAsync(int userId);
        Task UpsertAsync(UserProfile profile);
    }
}


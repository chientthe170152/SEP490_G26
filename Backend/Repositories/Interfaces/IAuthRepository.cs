using Backend.Models;

namespace Backend.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetUserByUsernameAsync(string username);
        Task<Role?> GetDefaultRoleAsync();
        Task<User> AddUserAsync(User user);
    }
}

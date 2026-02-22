using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implements
{
    public class AuthRepository : IAuthRepository
    {
        private readonly MtcaSep490G26Context _context;

        public AuthRepository(MtcaSep490G26Context context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<Role?> GetDefaultRoleAsync()
        {
            // E.g., looking up "User" or "Student"
            return await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User" || r.RoleName == "Student")
                   ?? await _context.Roles.FirstOrDefaultAsync();
        }

        public async Task<User> AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            // Reload user reference to get the explicit Role
            if (user.RoleId.HasValue)
            {
                await _context.Entry(user).Reference(u => u.Role).LoadAsync();
            }

            return user;
        }
    }
}

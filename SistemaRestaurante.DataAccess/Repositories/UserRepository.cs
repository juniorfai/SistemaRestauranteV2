using Microsoft.EntityFrameworkCore;
using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Repositories;

namespace SistemaRestaurante.DataAccess.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(RestaurantDbContext context) : base(context)
        {
        }

        public async Task<User?> GetForLoginAsync(string username)
        {
            return await _dbSet
                .IgnoreQueryFilters()
                .Include(u => u.Restaurant)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetForAuthAsync(int id)
        {
            return await _dbSet
                .IgnoreQueryFilters()
                .Include(u => u.Restaurant)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<IEnumerable<User>> GetAllIncludingInactiveAsync()
        {
            return await _dbSet
                .Include(u => u.Restaurant)
                .OrderByDescending(u => u.IsActive)
                .ThenBy(u => u.Restaurant != null ? u.Restaurant.Name : string.Empty)
                .ThenBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<User?> GetByIdIncludingInactiveAsync(int id)
        {
            return await _dbSet
                .Include(u => u.Restaurant)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<int> CountActiveByRoleAsync(UserRole role)
        {
            return await Active.CountAsync(u => u.Role == role);
        }

        public async Task<bool> AnyPlatformAdminAsync()
        {
            return await _dbSet.IgnoreQueryFilters().AnyAsync(u => u.Role == UserRole.PlatformAdmin);
        }
    }
}

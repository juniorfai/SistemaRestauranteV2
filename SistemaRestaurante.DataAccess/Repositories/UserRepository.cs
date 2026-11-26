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

        public async Task<User?> GetByUsernameIncludingInactiveAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<IEnumerable<User>> GetAllIncludingInactiveAsync()
        {
            return await _dbSet
                .OrderByDescending(u => u.IsActive)
                .ThenBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<User?> GetByIdIncludingInactiveAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<int> CountActiveByRoleAsync(UserRole role)
        {
            return await Active.CountAsync(u => u.Role == role);
        }

        public async Task<bool> AnyAsync()
        {
            return await _dbSet.AnyAsync();
        }
    }
}

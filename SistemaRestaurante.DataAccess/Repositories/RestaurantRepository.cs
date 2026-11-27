using Microsoft.EntityFrameworkCore;
using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Interfaces.Repositories;

namespace SistemaRestaurante.DataAccess.Repositories
{
    public class RestaurantRepository : GenericRepository<Restaurant>, IRestaurantRepository
    {
        public RestaurantRepository(RestaurantDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Restaurant>> GetAllIncludingInactiveAsync()
        {
            return await _dbSet
                .OrderByDescending(r => r.IsActive)
                .ThenBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<Restaurant?> GetByIdIncludingInactiveAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(r => r.Id == id);
        }
    }
}

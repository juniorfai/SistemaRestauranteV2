using Microsoft.EntityFrameworkCore;
using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Repositories;

namespace SistemaRestaurante.DataAccess.Repositories
{
    public class MenuItemRepository : GenericRepository<MenuItem>, IMenuItemRepository
    {
        public MenuItemRepository(RestaurantDbContext context) : base(context)
        {
        }

        // La zona se carga siempre: la carta enseña dónde se prepara cada plato.
        private IQueryable<MenuItem> WithStation => Active.Include(m => m.Station);

        public override async Task<MenuItem?> GetByIdAsync(int id)
        {
            return await WithStation.FirstOrDefaultAsync(m => m.Id == id);
        }

        public override async Task<IEnumerable<MenuItem>> GetAllAsync()
        {
            return await WithStation
                .OrderBy(m => m.Category).ThenBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<MenuItem>> GetByCategoryAsync(MenuItemCategory category)
        {
            return await WithStation
                .Where(m => m.Category == category && m.IsAvailable)
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<MenuItem>> GetAvailableItemsAsync()
        {
            return await WithStation
                .Where(m => m.IsAvailable)
                .OrderBy(m => m.Category).ThenBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<MenuItem>> GetByRestaurantAsync(int restaurantId)
        {
            return await WithStation
                .Where(m => m.RestaurantId == restaurantId && m.IsAvailable)
                .OrderBy(m => m.Category).ThenBy(m => m.Name)
                .ToListAsync();
        }
    }
}

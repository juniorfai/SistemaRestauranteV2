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

        public override async Task<IEnumerable<MenuItem>> GetAllAsync()
        {
            return await Active
                .OrderBy(m => m.Category).ThenBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<MenuItem>> GetByCategoryAsync(MenuItemCategory category)
        {
            return await Active
                .Where(m => m.Category == category && m.IsAvailable)
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<MenuItem>> GetAvailableItemsAsync()
        {
            return await Active
                .Where(m => m.IsAvailable)
                .OrderBy(m => m.Category).ThenBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<MenuItem>> GetByRestaurantAsync(int restaurantId)
        {
            return await Active
                .Where(m => m.RestaurantId == restaurantId && m.IsAvailable)
                .OrderBy(m => m.Category).ThenBy(m => m.Name)
                .ToListAsync();
        }
    }
}

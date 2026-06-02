using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SistemaRestaurante.DataAccess.Repositories
{
    public class MenuItemRepository : GenericRepository<MenuItem>, IMenuItemRepository
    {
        public MenuItemRepository(RestaurantDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<MenuItem>> GetByCategoryAsync(MenuItemCategory category)
        {
            return await _dbSet
                .Where(m => m.Category == category && m.IsAvailable)
                .ToListAsync();
        }

        public async Task<IEnumerable<MenuItem>> GetAvailableItemsAsync()
        {
            return await _dbSet
                .Where(m => m.IsAvailable)
                .ToListAsync();
        }

        public async Task<IEnumerable<MenuItem>> GetByRestaurantAsync(int restaurantId)
        {
            return await _dbSet
                .Where(m => m.RestaurantId == restaurantId && m.IsAvailable)
                .ToListAsync();
        }
    }
}

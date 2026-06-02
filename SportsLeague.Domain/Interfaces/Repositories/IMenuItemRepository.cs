using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.Domain.Interfaces.Repositories
{
    public interface IMenuItemRepository : IGenericRepository<MenuItem>
    {
        Task<IEnumerable<MenuItem>> GetByCategoryAsync(MenuItemCategory category);
        Task<IEnumerable<MenuItem>> GetAvailableItemsAsync();
        Task<IEnumerable<MenuItem>> GetByRestaurantAsync(int restaurantId);
    }
}

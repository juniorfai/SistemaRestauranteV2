using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.Domain.Interfaces.Repositories
{
    public interface IMenuItemRepository : IGenericRepository<MenuItem>
    {
        Task<IEnumerable<MenuItem>> GetByCategoryAsync(MenuItemCategory category);
        Task<IEnumerable<MenuItem>> GetAvailableItemsAsync();
        Task<IEnumerable<MenuItem>> GetByRestaurantAsync(int restaurantId);
    }
}

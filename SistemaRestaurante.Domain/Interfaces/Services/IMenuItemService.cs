using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.Domain.Interfaces.Services
{
    public interface IMenuItemService
    {
        Task<IEnumerable<MenuItem>> GetAllAsync(bool includeUnavailable);
        Task<MenuItem?> GetByIdAsync(int id);
        Task<IEnumerable<MenuItem>> GetByCategoryAsync(MenuItemCategory category);
        Task<IEnumerable<MenuItem>> GetByRestaurantAsync(int restaurantId);
        Task<MenuItem> CreateAsync(MenuItem item);
        Task UpdateAsync(int id, MenuItem item);
        Task DeleteAsync(int id);
    }
}

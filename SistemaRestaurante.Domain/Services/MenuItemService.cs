using Microsoft.Extensions.Logging;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using SistemaRestaurante.Domain.Interfaces.Services;

using SistemaRestaurante.Domain.Interfaces;

namespace SistemaRestaurante.Domain.Services
{
    public class MenuItemService : IMenuItemService
    {
        private readonly IMenuItemRepository _menuItemRepository;
        private readonly ITenantContext _tenant;
        private readonly ILogger<MenuItemService> _logger;

        public MenuItemService(
            IMenuItemRepository menuItemRepository,
            ITenantContext tenant,
            ILogger<MenuItemService> logger)
        {
            _menuItemRepository = menuItemRepository;
            _tenant = tenant;
            _logger = logger;
        }

        public Task<IEnumerable<MenuItem>> GetAllAsync(bool includeUnavailable) =>
            includeUnavailable ? _menuItemRepository.GetAllAsync() : _menuItemRepository.GetAvailableItemsAsync();

        public Task<MenuItem?> GetByIdAsync(int id) => _menuItemRepository.GetByIdAsync(id);

        public Task<IEnumerable<MenuItem>> GetByCategoryAsync(MenuItemCategory category) =>
            _menuItemRepository.GetByCategoryAsync(category);

        public Task<IEnumerable<MenuItem>> GetByRestaurantAsync(int restaurantId) =>
            _menuItemRepository.GetByRestaurantAsync(restaurantId);

        public async Task<MenuItem> CreateAsync(MenuItem item)
        {
            Validate(item);

            item.Id = 0;
            item.RestaurantId = _tenant.RequireRestaurantId();
            item.CreatedAt = DateTime.UtcNow;
            item.IsActive = true;
            _logger.LogInformation("Creating menu item {Name}", item.Name);
            return await _menuItemRepository.CreateAsync(item);
        }

        public async Task UpdateAsync(int id, MenuItem item)
        {
            var existing = await _menuItemRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró el plato con ID {id}");

            Validate(item);

            existing.Name = item.Name;
            existing.Description = item.Description;
            existing.Price = item.Price;
            existing.Category = item.Category;
            existing.IsAvailable = item.IsAvailable;
            existing.UpdatedAt = DateTime.UtcNow;
            await _menuItemRepository.UpdateAsync(existing);
        }

        public async Task DeleteAsync(int id)
        {
            if (!await _menuItemRepository.ExistsAsync(id))
                throw new KeyNotFoundException($"No se encontró el plato con ID {id}");

            _logger.LogInformation("Deleting menu item {MenuItemId}", id);
            await _menuItemRepository.DeleteAsync(id);
        }

        private static void Validate(MenuItem item)
        {
            if (item.Price <= 0)
                throw new ArgumentException("El precio debe ser mayor que cero");
        }
    }
}

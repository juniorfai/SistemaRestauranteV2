using Microsoft.Extensions.Logging;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.Domain.Services
{
    public class RestaurantService : IRestaurantService
    {
        private readonly IRestaurantRepository _restaurantRepository;
        private readonly IUserService _userService;
        private readonly ITenantContext _tenant;
        private readonly ILogger<RestaurantService> _logger;

        public RestaurantService(
            IRestaurantRepository restaurantRepository,
            IUserService userService,
            ITenantContext tenant,
            ILogger<RestaurantService> logger)
        {
            _restaurantRepository = restaurantRepository;
            _userService = userService;
            _tenant = tenant;
            _logger = logger;
        }

        public Task<IEnumerable<Restaurant>> GetAllAsync() => _restaurantRepository.GetAllIncludingInactiveAsync();

        public Task<Restaurant?> GetByIdAsync(int id) => _restaurantRepository.GetByIdIncludingInactiveAsync(id);

        public async Task<Restaurant> GetCurrentAsync()
        {
            var id = _tenant.RequireRestaurantId();
            return await _restaurantRepository.GetByIdIncludingInactiveAsync(id)
                ?? throw new KeyNotFoundException("No se encontró tu restaurante");
        }

        public async Task<Restaurant> CreateWithAdminAsync(Restaurant restaurant, string adminUsername, string adminFullName, string adminPassword)
        {
            // Se valida el administrador antes de crear nada, para no dejar un restaurante sin usuarios.
            await _userService.EnsureCanCreateAsync(adminUsername, adminPassword);

            restaurant.Id = 0;
            restaurant.CreatedAt = DateTime.UtcNow;
            restaurant.IsActive = true;
            Trim(restaurant, restaurant);
            var created = await _restaurantRepository.CreateAsync(restaurant);

            await _userService.CreateForRestaurantAsync(created.Id, adminUsername, adminFullName, UserRole.Admin, adminPassword);

            _logger.LogInformation("Created restaurant {RestaurantId} ({Name}) with admin {Username}",
                created.Id, created.Name, adminUsername);
            return created;
        }

        public async Task UpdateAsync(int id, Restaurant restaurant)
        {
            var existing = await _restaurantRepository.GetByIdIncludingInactiveAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró el restaurante con ID {id}");

            Trim(restaurant, existing);
            existing.UpdatedAt = DateTime.UtcNow;
            await _restaurantRepository.UpdateAsync(existing);
        }

        public async Task SetActiveAsync(int id, bool isActive)
        {
            var existing = await _restaurantRepository.GetByIdIncludingInactiveAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró el restaurante con ID {id}");

            existing.IsActive = isActive;
            existing.UpdatedAt = DateTime.UtcNow;
            await _restaurantRepository.UpdateAsync(existing);
            _logger.LogWarning("Restaurant {RestaurantId} {Action}", id, isActive ? "reactivated" : "suspended");
        }

        private static void Trim(Restaurant source, Restaurant target)
        {
            target.Name = source.Name.Trim();
            target.Address = source.Address.Trim();
            target.PhoneNumber = source.PhoneNumber.Trim();
            target.Email = source.Email.Trim().ToLowerInvariant();
            target.OpeningHours = string.IsNullOrWhiteSpace(source.OpeningHours) ? null : source.OpeningHours.Trim();
        }
    }
}

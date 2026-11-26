using Microsoft.Extensions.Logging;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.Domain.Services
{
    public class RestaurantService : IRestaurantService
    {
        private readonly IRestaurantRepository _restaurantRepository;
        private readonly ILogger<RestaurantService> _logger;

        public RestaurantService(IRestaurantRepository restaurantRepository, ILogger<RestaurantService> logger)
        {
            _restaurantRepository = restaurantRepository;
            _logger = logger;
        }

        public Task<IEnumerable<Restaurant>> GetAllAsync() => _restaurantRepository.GetAllAsync();

        public Task<Restaurant?> GetByIdAsync(int id) => _restaurantRepository.GetByIdAsync(id);

        public async Task<Restaurant> CreateAsync(Restaurant restaurant)
        {
            restaurant.Id = 0;
            restaurant.CreatedAt = DateTime.UtcNow;
            restaurant.IsActive = true;
            _logger.LogInformation("Creating restaurant {Name}", restaurant.Name);
            return await _restaurantRepository.CreateAsync(restaurant);
        }

        public async Task UpdateAsync(int id, Restaurant restaurant)
        {
            var existing = await _restaurantRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró el restaurante con ID {id}");

            existing.Name = restaurant.Name;
            existing.Address = restaurant.Address;
            existing.PhoneNumber = restaurant.PhoneNumber;
            existing.Email = restaurant.Email;
            existing.OpeningHours = restaurant.OpeningHours;
            existing.UpdatedAt = DateTime.UtcNow;
            await _restaurantRepository.UpdateAsync(existing);
        }
    }
}

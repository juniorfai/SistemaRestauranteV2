using SistemaRestaurante.Domain.Entities;

namespace SistemaRestaurante.Domain.Interfaces.Services
{
    public interface IRestaurantService
    {
        Task<IEnumerable<Restaurant>> GetAllAsync();
        Task<Restaurant?> GetByIdAsync(int id);
        Task<Restaurant> CreateAsync(Restaurant restaurant);
        Task UpdateAsync(int id, Restaurant restaurant);
    }
}

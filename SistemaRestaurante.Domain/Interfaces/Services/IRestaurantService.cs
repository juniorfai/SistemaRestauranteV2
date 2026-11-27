using SistemaRestaurante.Domain.Entities;

namespace SistemaRestaurante.Domain.Interfaces.Services
{
    public interface IRestaurantService
    {
        /// <summary>Todos los restaurantes, también los suspendidos (administrador de la plataforma).</summary>
        Task<IEnumerable<Restaurant>> GetAllAsync();

        Task<Restaurant?> GetByIdAsync(int id);

        /// <summary>Restaurante del usuario actual.</summary>
        Task<Restaurant> GetCurrentAsync();

        /// <summary>Alta de un restaurante con su primer administrador.</summary>
        Task<Restaurant> CreateWithAdminAsync(Restaurant restaurant, string adminUsername, string adminFullName, string adminPassword);

        Task UpdateAsync(int id, Restaurant restaurant);

        /// <summary>Suspende o reactiva un restaurante. Suspendido, su personal no puede entrar.</summary>
        Task SetActiveAsync(int id, bool isActive);
    }
}

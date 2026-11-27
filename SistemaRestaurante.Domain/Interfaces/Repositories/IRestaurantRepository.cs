using SistemaRestaurante.Domain.Entities;

namespace SistemaRestaurante.Domain.Interfaces.Repositories
{
    public interface IRestaurantRepository : IGenericRepository<Restaurant>
    {
        /// <summary>Restaurantes visibles, también los suspendidos (solo el administrador de la plataforma ve varios).</summary>
        Task<IEnumerable<Restaurant>> GetAllIncludingInactiveAsync();

        Task<Restaurant?> GetByIdIncludingInactiveAsync(int id);
    }
}

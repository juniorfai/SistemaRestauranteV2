using SistemaRestaurante.Domain.Entities;

namespace SistemaRestaurante.Domain.Interfaces.Repositories
{
    public interface ITableRepository : IGenericRepository<Table>
    {
        Task<IEnumerable<Table>> GetAvailableTablesAsync(DateTime dateTime, int numberOfPeople);
        Task<IEnumerable<Table>> GetTablesByRestaurantAsync(int restaurantId);
        Task<Table?> GetTableWithReservationsAsync(int id);

        /// <summary>Busca una mesa por número en un restaurante, incluyendo mesas dadas de baja.</summary>
        Task<Table?> GetByNumberIncludingInactiveAsync(int restaurantId, int tableNumber);
    }
}

using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.Domain.Interfaces.Services
{
    public interface ITableService
    {
        Task<IEnumerable<Table>> GetAllAsync();
        Task<Table?> GetByIdAsync(int id);
        Task<IEnumerable<Table>> GetAvailableAsync(DateTime dateTime, int numberOfPeople);
        Task<IEnumerable<Table>> GetByRestaurantAsync(int restaurantId);
        Task<Table> CreateAsync(Table table);
        Task UpdateAsync(int id, Table table);
        Task UpdateStatusAsync(int id, TableStatus status);
        Task DeleteAsync(int id);
    }
}

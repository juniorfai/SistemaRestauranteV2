using SistemaRestaurante.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SistemaRestaurante.Domain.Interfaces.Repositories
{
    public interface ITableRepository : IGenericRepository<Table>
    {
        Task<IEnumerable<Table>> GetAvailableTablesAsync(DateTime dateTime, int numberOfPeople);
        Task<IEnumerable<Table>> GetTablesByRestaurantAsync(int restaurantId);
        Task<Table?> GetTableWithReservationsAsync(int id);
    }
}

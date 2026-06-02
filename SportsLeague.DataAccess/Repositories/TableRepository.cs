using Microsoft.EntityFrameworkCore;
using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using SistemaRestaurante.Domain.Entities;
using System.Threading.Tasks;


namespace SistemaRestaurante.DataAccess.Repositories
{
    public class TableRepository : GenericRepository<Table>, ITableRepository
    {
        public TableRepository(RestaurantDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Table>> GetAvailableTablesAsync(DateTime dateTime, int numberOfPeople)
        {
            // Tablas que están disponibles y tienen capacidad suficiente
            var availableTables = await _dbSet
                .Where(t => t.Status == TableStatus.Available && t.Capacity >= numberOfPeople)
                .ToListAsync();

            // Filtrar tablas que no tienen reservas conflictivas
            var tablesWithReservations = await _dbSet
                .Include(t => t.Reservations)
                .Where(t => t.Reservations.Any(r =>
                    r.ReservationDateTime.Date == dateTime.Date &&
                    Math.Abs((r.ReservationDateTime - dateTime).TotalHours) < 2 &&
                    r.Status != ReservationStatus.Cancelled &&
                    r.Status != ReservationStatus.Completed))
                .Select(t => t.Id)
                .ToListAsync();

            return availableTables.Where(t => !tablesWithReservations.Contains(t.Id));
        }

        public async Task<IEnumerable<Table>> GetTablesByRestaurantAsync(int restaurantId)
        {
            return await _dbSet
                .Where(t => t.RestaurantId == restaurantId)
                .ToListAsync();
        }

        public async Task<Table?> GetTableWithReservationsAsync(int id)
        {
            return await _dbSet
                .Include(t => t.Reservations)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
    }
}

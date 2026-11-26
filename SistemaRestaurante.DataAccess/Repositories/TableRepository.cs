using Microsoft.EntityFrameworkCore;
using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.Domain.Constants;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Repositories;

namespace SistemaRestaurante.DataAccess.Repositories
{
    public class TableRepository : GenericRepository<Table>, ITableRepository
    {
        public TableRepository(RestaurantDbContext context) : base(context)
        {
        }

        public override async Task<IEnumerable<Table>> GetAllAsync()
        {
            return await Active
                .Include(t => t.Restaurant)
                .OrderBy(t => t.RestaurantId).ThenBy(t => t.TableNumber)
                .ToListAsync();
        }

        public override async Task<Table?> GetByIdAsync(int id)
        {
            return await Active
                .Include(t => t.Restaurant)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<Table>> GetAvailableTablesAsync(DateTime dateTime, int numberOfPeople)
        {
            // Una reserva ocupa la mesa durante ReservationRules.Duration, así que dos reservas chocan
            // si sus horas de inicio están a menos de esa duración. El rango se calcula aquí para que
            // EF Core pueda traducir la consulta a SQL (Math.Abs sobre TimeSpan no se traduce).
            var windowStart = dateTime - ReservationRules.Duration;
            var windowEnd = dateTime + ReservationRules.Duration;

            return await Active
                .Include(t => t.Restaurant)
                .Where(t => t.Status != TableStatus.OutOfService && t.Capacity >= numberOfPeople)
                .Where(t => !t.Reservations.Any(r =>
                    r.IsActive &&
                    (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed) &&
                    r.ReservationDateTime > windowStart &&
                    r.ReservationDateTime < windowEnd))
                .OrderBy(t => t.Capacity).ThenBy(t => t.TableNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Table>> GetTablesByRestaurantAsync(int restaurantId)
        {
            return await Active
                .Include(t => t.Restaurant)
                .Where(t => t.RestaurantId == restaurantId)
                .OrderBy(t => t.TableNumber)
                .ToListAsync();
        }

        public async Task<Table?> GetTableWithReservationsAsync(int id)
        {
            return await Active
                .Include(t => t.Reservations.Where(r => r.IsActive))
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Table?> GetByNumberIncludingInactiveAsync(int restaurantId, int tableNumber)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.RestaurantId == restaurantId && t.TableNumber == tableNumber);
        }
    }
}

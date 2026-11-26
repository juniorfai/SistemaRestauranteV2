using System.Data;
using Microsoft.EntityFrameworkCore;
using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.Domain.Constants;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Repositories;

namespace SistemaRestaurante.DataAccess.Repositories
{
    public class ReservationRepository : GenericRepository<Reservation>, IReservationRepository
    {
        public ReservationRepository(RestaurantDbContext context) : base(context)
        {
        }

        private IQueryable<Reservation> WithCustomerAndTable =>
            Active.Include(r => r.Customer).Include(r => r.Table);

        private static bool IsOpen(ReservationStatus status) =>
            status == ReservationStatus.Pending || status == ReservationStatus.Confirmed;

        public async Task<IEnumerable<Reservation>> GetReservationsByCustomerAsync(int customerId)
        {
            return await WithCustomerAndTable
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.ReservationDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByDateAsync(DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            return await WithCustomerAndTable
                .Where(r => r.ReservationDateTime >= dayStart && r.ReservationDateTime < dayEnd)
                .OrderBy(r => r.ReservationDateTime)
                .ToListAsync();
        }

        public async Task<Reservation?> GetReservationWithDetailsAsync(int id)
        {
            return await Active
                .Include(r => r.Customer)
                .Include(r => r.Table)
                    .ThenInclude(t => t.Restaurant)
                .Include(r => r.ReservationMenuItems)
                    .ThenInclude(rm => rm.MenuItem)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Reservation>> GetActiveReservationsAsync()
        {
            // Reservas abiertas desde hoy en adelante (incluye las de hoy que ya empezaron).
            var today = DateTime.Today;
            return await WithCustomerAndTable
                .Where(r => r.ReservationDateTime >= today &&
                            (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed))
                .OrderBy(r => r.ReservationDateTime)
                .ToListAsync();
        }

        public async Task<bool> HasConflictAsync(int tableId, DateTime dateTime, int? excludeReservationId = null)
        {
            var windowStart = dateTime - ReservationRules.Duration;
            var windowEnd = dateTime + ReservationRules.Duration;

            return await Active.AnyAsync(r =>
                r.TableId == tableId &&
                r.Id != (excludeReservationId ?? 0) &&
                (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed) &&
                r.ReservationDateTime > windowStart &&
                r.ReservationDateTime < windowEnd);
        }

        public async Task<bool> CustomerHasUpcomingReservationsAsync(int customerId)
        {
            var now = DateTime.Now;
            return await Active.AnyAsync(r =>
                r.CustomerId == customerId &&
                r.ReservationDateTime >= now &&
                (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed));
        }

        public async Task<bool> TableHasUpcomingReservationsAsync(int tableId)
        {
            var now = DateTime.Now;
            return await Active.AnyAsync(r =>
                r.TableId == tableId &&
                r.ReservationDateTime >= now &&
                (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed));
        }

        public async Task<Reservation> SaveIfTableFreeAsync(Reservation reservation)
        {
            // Serializable bloquea el rango consultado hasta el commit, así dos peticiones simultáneas
            // no pueden reservar la misma mesa en el mismo horario.
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var excludeId = reservation.Id == 0 ? (int?)null : reservation.Id;
            if (IsOpen(reservation.Status) &&
                await HasConflictAsync(reservation.TableId, reservation.ReservationDateTime, excludeId))
            {
                throw new InvalidOperationException("La mesa ya tiene una reserva cerca de ese horario");
            }

            if (reservation.Id == 0)
                await _dbSet.AddAsync(reservation);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return reservation;
        }
    }
}

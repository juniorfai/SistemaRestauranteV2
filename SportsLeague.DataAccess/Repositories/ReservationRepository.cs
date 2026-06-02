using Microsoft.EntityFrameworkCore;
using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.DataAccess.Repositories
{
    public class ReservationRepository : GenericRepository<Reservation>, IReservationRepository
    {
        public ReservationRepository(RestaurantDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByCustomerAsync(int customerId)
        {
            return await _dbSet
                .Where(r => r.CustomerId == customerId)
                .Include(r => r.Table)
                .OrderByDescending(r => r.ReservationDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByDateAsync(DateTime date)
        {
            return await _dbSet
                .Where(r => r.ReservationDateTime.Date == date.Date)
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .ToListAsync();
        }

        public async Task<Reservation?> GetReservationWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .ThenInclude(t => t.Restaurant)
                .Include(r => r.ReservationMenuItems)
                .ThenInclude(rm => rm.MenuItem)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Reservation>> GetActiveReservationsAsync()
        {
            return await _dbSet
                .Where(r => r.ReservationDateTime >= DateTime.UtcNow &&
                            r.Status != ReservationStatus.Cancelled &&
                            r.Status != ReservationStatus.Completed)
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .OrderBy(r => r.ReservationDateTime)
                .ToListAsync();
        }
    }
}

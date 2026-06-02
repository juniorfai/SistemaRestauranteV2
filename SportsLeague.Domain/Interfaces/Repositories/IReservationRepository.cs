using SistemaRestaurante.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.Domain.Interfaces.Repositories
{
    public interface IReservationRepository : IGenericRepository<Reservation>
    {
        Task<IEnumerable<Reservation>> GetReservationsByCustomerAsync(int customerId);
        Task<IEnumerable<Reservation>> GetReservationsByDateAsync(DateTime date);
        Task<Reservation?> GetReservationWithDetailsAsync(int id);
        Task<IEnumerable<Reservation>> GetActiveReservationsAsync();
    }
}

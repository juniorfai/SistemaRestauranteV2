using SistemaRestaurante.Domain.Entities;

namespace SistemaRestaurante.Domain.Interfaces.Repositories
{
    public interface IReservationRepository : IGenericRepository<Reservation>
    {
        Task<IEnumerable<Reservation>> GetReservationsByCustomerAsync(int customerId);
        Task<IEnumerable<Reservation>> GetReservationsByDateAsync(DateTime date);
        Task<Reservation?> GetReservationWithDetailsAsync(int id);
        Task<IEnumerable<Reservation>> GetActiveReservationsAsync();

        /// <summary>Indica si la mesa ya tiene una reserva vigente que se cruce con ese horario.</summary>
        Task<bool> HasConflictAsync(int tableId, DateTime dateTime, int? excludeReservationId = null);

        /// <summary>Indica si el cliente tiene reservas futuras pendientes o confirmadas.</summary>
        Task<bool> CustomerHasUpcomingReservationsAsync(int customerId);

        /// <summary>Indica si la mesa tiene reservas futuras pendientes o confirmadas.</summary>
        Task<bool> TableHasUpcomingReservationsAsync(int tableId);

        /// <summary>
        /// Guarda la reserva (nueva o existente) solo si la mesa sigue libre, dentro de una transacción
        /// serializable para evitar dobles reservas simultáneas. Lanza InvalidOperationException si hay choque.
        /// </summary>
        Task<Reservation> SaveIfTableFreeAsync(Reservation reservation);
    }
}

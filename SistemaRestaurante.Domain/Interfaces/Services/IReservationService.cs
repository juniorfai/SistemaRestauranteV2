using SistemaRestaurante.Domain.Entities;

namespace SistemaRestaurante.Domain.Interfaces.Services
{
    public interface IReservationService
    {
        Task<IEnumerable<Reservation>> GetAllAsync();
        Task<Reservation?> GetByIdAsync(int id);
        Task<Reservation> CreateAsync(Reservation reservation, List<int> menuItemIds, List<int> quantities);
        Task<Reservation> UpdateAsync(int id, Reservation reservation);
        Task DeleteAsync(int id);
        Task CancelReservationAsync(int id);
        Task ConfirmReservationAsync(int id);
        Task CompleteReservationAsync(int id);
        Task MarkNoShowAsync(int id);
        Task<IEnumerable<Reservation>> GetByCustomerAsync(int customerId);
        Task<IEnumerable<Reservation>> GetByDateAsync(DateTime date);
    }
}

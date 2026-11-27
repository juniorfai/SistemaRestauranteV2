using Microsoft.Extensions.Logging;
using SistemaRestaurante.Domain.Constants;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.Domain.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ITableRepository _tableRepository;
        private readonly IMenuItemRepository _menuItemRepository;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(
            IReservationRepository reservationRepository,
            ICustomerRepository customerRepository,
            ITableRepository tableRepository,
            IMenuItemRepository menuItemRepository,
            ILogger<ReservationService> logger)
        {
            _reservationRepository = reservationRepository;
            _customerRepository = customerRepository;
            _tableRepository = tableRepository;
            _menuItemRepository = menuItemRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<Reservation>> GetAllAsync()
        {
            return await _reservationRepository.GetActiveReservationsAsync();
        }

        public async Task<Reservation?> GetByIdAsync(int id)
        {
            return await _reservationRepository.GetReservationWithDetailsAsync(id);
        }

        public async Task<Reservation> CreateAsync(Reservation reservation, List<int> menuItemIds, List<int> quantities)
        {
            if (menuItemIds.Count != quantities.Count)
                throw new ArgumentException("La lista de platos y la de cantidades deben tener el mismo tamaño");

            var customer = await _customerRepository.GetByIdAsync(reservation.CustomerId)
                ?? throw new KeyNotFoundException($"No se encontró el cliente con ID {reservation.CustomerId}");

            var table = await ValidateTableAndScheduleAsync(reservation.TableId, reservation.ReservationDateTime, reservation.NumberOfPeople);

            decimal total = 0;
            var reservationMenuItems = new List<ReservationMenuItem>();
            for (int i = 0; i < menuItemIds.Count; i++)
            {
                if (quantities[i] <= 0)
                    throw new ArgumentException("Las cantidades de los platos deben ser mayores que cero");

                var menuItem = await _menuItemRepository.GetByIdAsync(menuItemIds[i])
                    ?? throw new KeyNotFoundException($"No se encontró el plato del menú con ID {menuItemIds[i]}");

                if (!menuItem.IsAvailable)
                    throw new InvalidOperationException($"El plato \"{menuItem.Name}\" no está disponible");

                if (menuItem.RestaurantId != table.RestaurantId)
                    throw new InvalidOperationException($"El plato \"{menuItem.Name}\" no pertenece al restaurante de la mesa");

                var existing = reservationMenuItems.FirstOrDefault(rm => rm.MenuItemId == menuItem.Id);
                if (existing != null)
                {
                    existing.Quantity += quantities[i];
                }
                else
                {
                    reservationMenuItems.Add(new ReservationMenuItem
                    {
                        MenuItemId = menuItem.Id,
                        Quantity = quantities[i],
                        UnitPrice = menuItem.Price
                    });
                }
                total += menuItem.Price * quantities[i];
            }

            reservation.Id = 0;
            reservation.RestaurantId = table.RestaurantId;
            reservation.TotalAmount = total;
            reservation.Status = ReservationStatus.Pending;
            reservation.ReservationMenuItems = reservationMenuItems;
            reservation.CreatedAt = DateTime.UtcNow;
            reservation.IsActive = true;

            _logger.LogInformation("Creating reservation for customer {CustomerId} at table {TableId}",
                customer.Id, table.Id);

            return await _reservationRepository.SaveIfTableFreeAsync(reservation);
        }

        public async Task<Reservation> UpdateAsync(int id, Reservation reservation)
        {
            var existing = await _reservationRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró la reserva con ID {id}");

            EnsureStatus(existing, "modificar", ReservationStatus.Pending, ReservationStatus.Confirmed);

            await ValidateTableAndScheduleAsync(reservation.TableId, reservation.ReservationDateTime, reservation.NumberOfPeople);

            existing.TableId = reservation.TableId;
            existing.ReservationDateTime = reservation.ReservationDateTime;
            existing.NumberOfPeople = reservation.NumberOfPeople;
            existing.SpecialRequests = reservation.SpecialRequests;
            existing.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Updating reservation with ID: {ReservationId}", id);
            return await _reservationRepository.SaveIfTableFreeAsync(existing);
        }

        public async Task DeleteAsync(int id)
        {
            if (!await _reservationRepository.ExistsAsync(id))
                throw new KeyNotFoundException($"No se encontró la reserva con ID {id}");

            _logger.LogInformation("Deleting reservation with ID: {ReservationId}", id);
            await _reservationRepository.DeleteAsync(id);
        }

        public Task CancelReservationAsync(int id) =>
            ChangeStatusAsync(id, ReservationStatus.Cancelled, "cancelar",
                ReservationStatus.Pending, ReservationStatus.Confirmed);

        public Task ConfirmReservationAsync(int id) =>
            ChangeStatusAsync(id, ReservationStatus.Confirmed, "confirmar",
                ReservationStatus.Pending);

        public Task CompleteReservationAsync(int id) =>
            ChangeStatusAsync(id, ReservationStatus.Completed, "completar",
                ReservationStatus.Pending, ReservationStatus.Confirmed);

        public Task MarkNoShowAsync(int id) =>
            ChangeStatusAsync(id, ReservationStatus.NoShow, "marcar como no asistida",
                ReservationStatus.Pending, ReservationStatus.Confirmed);

        public async Task<IEnumerable<Reservation>> GetByCustomerAsync(int customerId)
        {
            if (!await _customerRepository.ExistsAsync(customerId))
                throw new KeyNotFoundException($"No se encontró el cliente con ID {customerId}");

            return await _reservationRepository.GetReservationsByCustomerAsync(customerId);
        }

        public async Task<IEnumerable<Reservation>> GetByDateAsync(DateTime date)
        {
            return await _reservationRepository.GetReservationsByDateAsync(date);
        }

        private async Task<Table> ValidateTableAndScheduleAsync(int tableId, DateTime dateTime, int numberOfPeople)
        {
            if (numberOfPeople < 1 || numberOfPeople > ReservationRules.MaxPeoplePerReservation)
                throw new ArgumentException($"El número de personas debe estar entre 1 y {ReservationRules.MaxPeoplePerReservation}");

            if (dateTime <= DateTime.Now)
                throw new ArgumentException("La fecha de la reserva debe ser futura");

            var table = await _tableRepository.GetByIdAsync(tableId)
                ?? throw new KeyNotFoundException($"No se encontró la mesa con ID {tableId}");

            if (table.Status == TableStatus.OutOfService)
                throw new InvalidOperationException($"La mesa {table.TableNumber} está fuera de servicio");

            if (table.Capacity < numberOfPeople)
                throw new InvalidOperationException(
                    $"La mesa {table.TableNumber} tiene capacidad para {table.Capacity} personas, no para {numberOfPeople}");

            return table;
        }

        private async Task ChangeStatusAsync(int id, ReservationStatus newStatus, string action,
            params ReservationStatus[] allowedFrom)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró la reserva con ID {id}");

            EnsureStatus(reservation, action, allowedFrom);

            reservation.Status = newStatus;
            reservation.UpdatedAt = DateTime.UtcNow;
            await _reservationRepository.UpdateAsync(reservation);

            _logger.LogInformation("Reservation {ReservationId} changed to {Status}", id, newStatus);
        }

        private static void EnsureStatus(Reservation reservation, string action, params ReservationStatus[] allowed)
        {
            if (!allowed.Contains(reservation.Status))
                throw new InvalidOperationException(
                    $"No se puede {action} una reserva en estado {reservation.Status}");
        }
    }
}

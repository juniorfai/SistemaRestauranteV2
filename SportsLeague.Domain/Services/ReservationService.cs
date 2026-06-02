using Microsoft.Extensions.Logging;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using SistemaRestaurante.Domain.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

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
            _logger.LogInformation("Retrieving all reservations");
            return await _reservationRepository.GetActiveReservationsAsync();
        }

        public async Task<Reservation?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Retrieving reservation with ID: {ReservationId}", id);
            return await _reservationRepository.GetReservationWithDetailsAsync(id);
        }

        public async Task<Reservation> CreateAsync(Reservation reservation, List<int> menuItemIds, List<int> quantities)
        {
            // Validar que el cliente existe
            var customer = await _customerRepository.GetByIdAsync(reservation.CustomerId);
            if (customer == null)
            {
                throw new KeyNotFoundException($"No se encontró el cliente con ID {reservation.CustomerId}");
            }

            // Validar que la mesa existe y está disponible
            var table = await _tableRepository.GetTableWithReservationsAsync(reservation.TableId);
            if (table == null)
            {
                throw new KeyNotFoundException($"No se encontró la mesa con ID {reservation.TableId}");
            }

            // Validar capacidad de la mesa
            if (table.Capacity < reservation.NumberOfPeople)
            {
                throw new InvalidOperationException($"La mesa {table.TableNumber} tiene capacidad para {table.Capacity} personas, no para {reservation.NumberOfPeople}");
            }

            // Validar que la mesa no tenga reserva en ese horario
            var existingReservations = await _reservationRepository.GetReservationsByDateAsync(reservation.ReservationDateTime.Date);
            var conflict = existingReservations.Any(r =>
                r.TableId == reservation.TableId &&
                Math.Abs((r.ReservationDateTime - reservation.ReservationDateTime).TotalHours) < 2 &&
                r.Status != ReservationStatus.Cancelled &&
                r.Status != ReservationStatus.Completed);

            if (conflict)
            {
                throw new InvalidOperationException($"La mesa {table.TableNumber} ya tiene una reserva cerca de ese horario");
            }

            // Calcular total si hay items del menú
            decimal total = 0;
            var reservationMenuItems = new List<ReservationMenuItem>();

            for (int i = 0; i < menuItemIds.Count; i++)
            {
                var menuItem = await _menuItemRepository.GetByIdAsync(menuItemIds[i]);
                if (menuItem == null)
                {
                    throw new KeyNotFoundException($"No se encontró el item del menú con ID {menuItemIds[i]}");
                }

                total += menuItem.Price * quantities[i];
                reservationMenuItems.Add(new ReservationMenuItem
                {
                    MenuItemId = menuItem.Id,
                    Quantity = quantities[i],
                    UnitPrice = menuItem.Price
                });
            }

            reservation.TotalAmount = total;
            reservation.Status = ReservationStatus.Pending;
            reservation.ReservationMenuItems = reservationMenuItems;

            _logger.LogInformation("Creating reservation for customer {CustomerId} at table {TableId}",
                reservation.CustomerId, reservation.TableId);

            return await _reservationRepository.CreateAsync(reservation);
        }

        public async Task UpdateAsync(int id, Reservation reservation)
        {
            var existingReservation = await _reservationRepository.GetReservationWithDetailsAsync(id);
            if (existingReservation == null)
            {
                throw new KeyNotFoundException($"No se encontró la reserva con ID {id}");
            }

            // No permitir modificar reservas completadas o canceladas
            if (existingReservation.Status == ReservationStatus.Completed ||
                existingReservation.Status == ReservationStatus.Cancelled)
            {
                throw new InvalidOperationException($"No se puede modificar una reserva {existingReservation.Status}");
            }

            existingReservation.ReservationDateTime = reservation.ReservationDateTime;
            existingReservation.NumberOfPeople = reservation.NumberOfPeople;
            existingReservation.SpecialRequests = reservation.SpecialRequests;
            existingReservation.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Updating reservation with ID: {ReservationId}", id);
            await _reservationRepository.UpdateAsync(existingReservation);
        }

        public async Task DeleteAsync(int id)
        {
            var exists = await _reservationRepository.ExistsAsync(id);
            if (!exists)
            {
                throw new KeyNotFoundException($"No se encontró la reserva con ID {id}");
            }

            _logger.LogInformation("Deleting reservation with ID: {ReservationId}", id);
            await _reservationRepository.DeleteAsync(id);
        }

        public async Task CancelReservationAsync(int id)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null)
            {
                throw new KeyNotFoundException($"No se encontró la reserva con ID {id}");
            }

            reservation.Status = ReservationStatus.Cancelled;
            reservation.UpdatedAt = DateTime.UtcNow;
            await _reservationRepository.UpdateAsync(reservation);

            _logger.LogInformation("Cancelled reservation with ID: {ReservationId}", id);
        }

        public async Task ConfirmReservationAsync(int id)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null)
            {
                throw new KeyNotFoundException($"No se encontró la reserva con ID {id}");
            }

            reservation.Status = ReservationStatus.Confirmed;
            reservation.UpdatedAt = DateTime.UtcNow;
            await _reservationRepository.UpdateAsync(reservation);

            _logger.LogInformation("Confirmed reservation with ID: {ReservationId}", id);
        }

        public async Task<IEnumerable<Reservation>> GetByCustomerAsync(int customerId)
        {
            var customerExists = await _customerRepository.ExistsAsync(customerId);
            if (!customerExists)
            {
                throw new KeyNotFoundException($"No se encontró el cliente con ID {customerId}");
            }

            return await _reservationRepository.GetReservationsByCustomerAsync(customerId);
        }

        public async Task<IEnumerable<Reservation>> GetByDateAsync(DateTime date)
        {
            return await _reservationRepository.GetReservationsByDateAsync(date);
        }
    }
}

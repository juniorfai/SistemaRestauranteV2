using Microsoft.Extensions.Logging;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using SistemaRestaurante.Domain.Interfaces.Services;

using SistemaRestaurante.Domain.Interfaces;

namespace SistemaRestaurante.Domain.Services
{
    public class TableService : ITableService
    {
        private readonly ITableRepository _tableRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly ITenantContext _tenant;
        private readonly ILogger<TableService> _logger;

        public TableService(
            ITableRepository tableRepository,
            IReservationRepository reservationRepository,
            ITenantContext tenant,
            ILogger<TableService> logger)
        {
            _tableRepository = tableRepository;
            _reservationRepository = reservationRepository;
            _tenant = tenant;
            _logger = logger;
        }

        public Task<IEnumerable<Table>> GetAllAsync() => _tableRepository.GetAllAsync();

        public Task<Table?> GetByIdAsync(int id) => _tableRepository.GetByIdAsync(id);

        public Task<IEnumerable<Table>> GetAvailableAsync(DateTime dateTime, int numberOfPeople)
        {
            if (numberOfPeople < 1)
                throw new ArgumentException("El número de personas debe ser mayor que cero");

            return _tableRepository.GetAvailableTablesAsync(dateTime, numberOfPeople);
        }

        public Task<IEnumerable<Table>> GetByRestaurantAsync(int restaurantId) =>
            _tableRepository.GetTablesByRestaurantAsync(restaurantId);

        public async Task<Table> CreateAsync(Table table)
        {
            table.RestaurantId = _tenant.RequireRestaurantId();

            // El número de mesa es único por restaurante (también entre mesas dadas de baja).
            var sameNumber = await _tableRepository.GetByNumberIncludingInactiveAsync(table.RestaurantId, table.TableNumber);
            if (sameNumber != null)
            {
                if (sameNumber.IsActive)
                    throw new InvalidOperationException($"Ya existe la mesa número {table.TableNumber} en este restaurante");

                // Reactivar la mesa dada de baja con los datos nuevos.
                sameNumber.Capacity = table.Capacity;
                sameNumber.Location = table.Location;
                sameNumber.Status = table.Status;
                sameNumber.IsActive = true;
                sameNumber.UpdatedAt = DateTime.UtcNow;
                await _tableRepository.UpdateAsync(sameNumber);
                _logger.LogInformation("Reactivated table {TableId}", sameNumber.Id);
                return sameNumber;
            }

            table.Id = 0;
            table.CreatedAt = DateTime.UtcNow;
            table.IsActive = true;
            _logger.LogInformation("Creating table {TableNumber} for restaurant {RestaurantId}", table.TableNumber, table.RestaurantId);
            return await _tableRepository.CreateAsync(table);
        }

        public async Task UpdateAsync(int id, Table table)
        {
            var existing = await _tableRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró la mesa con ID {id}");

            if (existing.TableNumber != table.TableNumber)
            {
                var sameNumber = await _tableRepository.GetByNumberIncludingInactiveAsync(existing.RestaurantId, table.TableNumber);
                if (sameNumber != null && sameNumber.Id != id)
                    throw new InvalidOperationException($"Ya existe la mesa número {table.TableNumber} en este restaurante");
            }

            existing.TableNumber = table.TableNumber;
            existing.Capacity = table.Capacity;
            existing.Location = table.Location;
            existing.Status = table.Status;
            existing.UpdatedAt = DateTime.UtcNow;
            await _tableRepository.UpdateAsync(existing);
        }

        public async Task UpdateStatusAsync(int id, TableStatus status)
        {
            var existing = await _tableRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró la mesa con ID {id}");

            existing.Status = status;
            existing.UpdatedAt = DateTime.UtcNow;
            await _tableRepository.UpdateAsync(existing);
        }

        public async Task DeleteAsync(int id)
        {
            if (!await _tableRepository.ExistsAsync(id))
                throw new KeyNotFoundException($"No se encontró la mesa con ID {id}");

            if (await _reservationRepository.TableHasUpcomingReservationsAsync(id))
                throw new InvalidOperationException("La mesa tiene reservas próximas. Cancélalas o muévelas antes de eliminarla");

            _logger.LogInformation("Deleting table {TableId}", id);
            await _tableRepository.DeleteAsync(id);
        }
    }
}

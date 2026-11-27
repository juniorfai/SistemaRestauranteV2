using Microsoft.Extensions.Logging;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.Domain.Services
{
    public class StationService : IStationService
    {
        private readonly IStationRepository _stations;
        private readonly ITenantContext _tenant;
        private readonly ILogger<StationService> _logger;

        public StationService(IStationRepository stations, ITenantContext tenant, ILogger<StationService> logger)
        {
            _stations = stations;
            _tenant = tenant;
            _logger = logger;
        }

        public async Task<IEnumerable<Station>> GetAllAsync() =>
            (await _stations.GetAllAsync()).OrderBy(s => s.SortOrder).ThenBy(s => s.Name);

        public Task<Station?> GetByIdAsync(int id) => _stations.GetByIdAsync(id);

        public async Task<Station> CreateAsync(Station station)
        {
            Normalize(station);
            await EnsureUniqueNameAsync(station.Name, null);
            station.Id = 0;
            station.RestaurantId = _tenant.RequireRestaurantId();
            station.CreatedAt = DateTime.UtcNow;
            station.IsActive = true;
            return await _stations.CreateAsync(station);
        }

        public async Task UpdateAsync(int id, Station station)
        {
            var existing = await _stations.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró la zona con ID {id}");
            Normalize(station);
            await EnsureUniqueNameAsync(station.Name, id);

            existing.Name = station.Name;
            existing.Type = station.Type;
            existing.SortOrder = station.SortOrder;
            existing.PrinterAddress = station.PrinterAddress;
            existing.UpdatedAt = DateTime.UtcNow;
            await _stations.UpdateAsync(existing);
        }

        public async Task DeleteAsync(int id)
        {
            if (!await _stations.ExistsAsync(id))
                throw new KeyNotFoundException($"No se encontró la zona con ID {id}");
            if (await _stations.CountAsync() <= 1)
                throw new InvalidOperationException("El restaurante necesita al menos una zona de preparación");
            var dishes = await _stations.CountMenuItemsAsync(id);
            if (dishes > 0)
                throw new InvalidOperationException($"Hay {dishes} plato(s) asignados a esta zona; muévelos antes a otra");

            await _stations.DeleteAsync(id);
        }

        public async Task CreateDefaultsForAsync(int restaurantId)
        {
            await _stations.CreateManyAsync(new[]
            {
                new Station { Name = "Cocina", Type = StationType.Kitchen, SortOrder = 1, RestaurantId = restaurantId },
                new Station { Name = "Barra", Type = StationType.Bar, SortOrder = 2, RestaurantId = restaurantId },
            });
            _logger.LogInformation("Created default stations for restaurant {RestaurantId}", restaurantId);
        }

        public async Task<int> ResolveForMenuItemAsync(int? stationId, MenuItemCategory category)
        {
            if (stationId is int id)
            {
                // El filtro por restaurante hace que una zona de otro restaurante "no exista".
                return (await _stations.GetByIdAsync(id))?.Id
                    ?? throw new KeyNotFoundException($"No se encontró la zona con ID {id}");
            }

            var preferred = category == MenuItemCategory.Drink ? StationType.Bar : StationType.Kitchen;
            var station = await _stations.GetFirstOfTypeAsync(preferred)
                ?? (await GetAllAsync()).FirstOrDefault()
                ?? throw new InvalidOperationException("El restaurante no tiene zonas de preparación; crea una primero");
            return station.Id;
        }

        private static void Normalize(Station station)
        {
            station.Name = station.Name.Trim();
            station.PrinterAddress = string.IsNullOrWhiteSpace(station.PrinterAddress) ? null : station.PrinterAddress.Trim();
            if (station.Name.Length == 0)
                throw new ArgumentException("El nombre de la zona es obligatorio");
        }

        private async Task EnsureUniqueNameAsync(string name, int? exceptId)
        {
            var all = await _stations.GetAllAsync();
            if (all.Any(s => s.Id != exceptId && string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Ya existe una zona llamada \"{name}\"");
        }
    }
}

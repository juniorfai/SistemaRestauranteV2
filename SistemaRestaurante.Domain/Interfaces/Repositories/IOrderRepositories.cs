using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.Domain.Interfaces.Repositories
{
    public interface IStationRepository : IGenericRepository<Station>
    {
        /// <summary>Zona por defecto de un tipo (la primera por orden), o null si no hay.</summary>
        Task<Station?> GetFirstOfTypeAsync(StationType type);
        Task<int> CountMenuItemsAsync(int stationId);
        Task<int> CountAsync();
        /// <summary>Crea las zonas de un restaurante nuevo (lo hace la plataforma, sin restaurante propio).</summary>
        Task CreateManyAsync(IEnumerable<Station> stations);
    }

    public interface IOrderRepository
    {
        /// <summary>Cuenta con sus platos (activos), mesa y camarero.</summary>
        Task<Order?> GetDetailAsync(int id);
        Task<IEnumerable<Order>> GetOpenAsync();
        Task<bool> HasOpenOrderForTableAsync(int tableId);
        Task<IEnumerable<Order>> GetClosedBetweenAsync(DateTime fromUtc, DateTime toUtc);

        /// <summary>Platos que la cocina tiene que ver: retenidos, enviados, en preparación y listos.</summary>
        Task<IEnumerable<OrderItem>> GetKitchenItemsAsync(int? stationId);

        Task<OrderItem?> GetItemAsync(int orderId, int itemId);

        Task<Order> CreateAsync(Order order);

        /// <summary>
        /// Guarda los cambios. Traduce los choques a InvalidOperationException (409): otra cuenta
        /// abierta en la misma mesa o un cambio simultáneo desde otro dispositivo.
        /// </summary>
        Task SaveAsync();
    }
}

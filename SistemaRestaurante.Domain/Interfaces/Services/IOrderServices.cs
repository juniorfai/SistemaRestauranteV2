using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.Domain.Interfaces.Services
{
    public interface IStationService
    {
        Task<IEnumerable<Station>> GetAllAsync();
        Task<Station?> GetByIdAsync(int id);
        Task<Station> CreateAsync(Station station);
        Task UpdateAsync(int id, Station station);
        Task DeleteAsync(int id);

        /// <summary>Cocina y Barra para un restaurante recién creado.</summary>
        Task CreateDefaultsForAsync(int restaurantId);

        /// <summary>Zona de un plato: la indicada o, si no, Barra para bebidas y Cocina para el resto.</summary>
        Task<int> ResolveForMenuItemAsync(int? stationId, MenuItemCategory category);
    }

    /// <summary>Un plato que se añade a una cuenta.</summary>
    public record NewOrderItem(int MenuItemId, int Quantity, string? Notes, OrderCourse? Course, Guid? ClientRequestId);

    /// <summary>Cuenta provisional que se enseña al cliente (no es una factura).</summary>
    public record Bill(Order Order, IReadOnlyList<OrderItem> Lines, decimal Subtotal, decimal SuggestedTipPercent,
        decimal SuggestedTip, decimal TotalWithSuggestedTip, string CurrencyCode, Restaurant Restaurant);

    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetOpenAsync();
        Task<Order> GetAsync(int id);
        Task<IEnumerable<Order>> GetClosedOnAsync(DateOnly localDate);

        Task<Order> OpenAsync(int tableId, int guests, int? reservationId, string? notes, int waiterId);
        Task<Order> AddItemsAsync(int orderId, IReadOnlyList<NewOrderItem> items);
        Task UpdateDraftItemAsync(int orderId, int itemId, int quantity, string? notes, OrderCourse course);
        Task RemoveDraftItemAsync(int orderId, int itemId);

        /// <summary>
        /// Envía a cocina lo apuntado. Con <paramref name="holdFromCourse"/>, los platos de ese
        /// tiempo o posteriores quedan retenidos hasta marcharlos.
        /// </summary>
        Task<Order> SendAsync(int orderId, OrderCourse? holdFromCourse);

        /// <summary>"Marchar": libera los platos retenidos hasta el tiempo indicado.</summary>
        Task<Order> FireAsync(int orderId, OrderCourse upToCourse);

        /// <summary>Cambio de estado de un plato desde cocina o sala, comprobando el estado esperado.</summary>
        Task<OrderItem> ChangeItemStatusAsync(int orderId, int itemId, OrderItemStatus newStatus, OrderItemStatus? expectedStatus);

        /// <summary>Cocina despacha la comanda de una zona: todo lo pendiente queda listo.</summary>
        Task<Order> BumpAsync(int orderId, int stationId);

        /// <summary>Anula un plato ya enviado (solo encargado o administrador, con motivo).</summary>
        Task CancelItemAsync(int orderId, int itemId, string reason, int userId);

        Task MoveToTableAsync(int orderId, int tableId);
        Task SetGuestsAsync(int orderId, int guests);

        Task<Bill> GetBillAsync(int orderId);
        Task<Order> CloseAsync(int orderId, PaymentMethod paymentMethod, decimal tipAmount, int userId);
        Task CancelAsync(int orderId, string reason, int userId);

        Task<IEnumerable<OrderItem>> GetKitchenItemsAsync(int? stationId);
    }

    /// <summary>Avisos en tiempo real a las pantallas de cocina y sala del restaurante.</summary>
    public interface IOrderNotifier
    {
        Task OrderChangedAsync(int restaurantId, int orderId);

        /// <summary>Cocina dejó platos listos: el camarero de la mesa recibe el aviso.</summary>
        Task ItemsReadyAsync(int restaurantId, int orderId, int tableNumber, int waiterId, IReadOnlyList<string> items);
    }
}

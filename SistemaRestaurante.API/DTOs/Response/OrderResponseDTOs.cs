using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.API.DTOs.Response
{
    public class StationResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public StationType Type { get; set; }
        public int SortOrder { get; set; }
        public string? PrinterAddress { get; set; }
    }

    public class OrderItemResponseDTO
    {
        public int Id { get; set; }
        public int MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
        public string? Notes { get; set; }
        public OrderCourse Course { get; set; }
        public OrderItemStatus Status { get; set; }
        public int StationId { get; set; }
        public string StationName { get; set; } = string.Empty;
        public Guid? ClientRequestId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? ReadyAt { get; set; }
        public DateTime? ServedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelReason { get; set; }
    }

    public class OrderResponseDTO
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        public int TableNumber { get; set; }
        public int WaiterId { get; set; }
        public string WaiterName { get; set; } = string.Empty;
        public int? ReservationId { get; set; }
        public int Guests { get; set; }
        public string? Notes { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        /// <summary>Abierta: lo pedido hasta ahora (sin anulados ni borradores). Cerrada: lo cobrado.</summary>
        public decimal Subtotal { get; set; }
        public decimal? TipAmount { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public string? CancelReason { get; set; }
        public List<OrderItemResponseDTO> Items { get; set; } = new();
    }

    /// <summary>Cuenta provisional para enseñar al cliente. No es una factura.</summary>
    public class BillResponseDTO
    {
        public int OrderId { get; set; }
        public int TableNumber { get; set; }
        public int Guests { get; set; }
        public string WaiterName { get; set; } = string.Empty;
        public DateTime OpenedAt { get; set; }
        public string CurrencyCode { get; set; } = "COP";
        public List<BillLineDTO> Lines { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal SuggestedTipPercent { get; set; }
        public decimal SuggestedTip { get; set; }
        public decimal TotalWithSuggestedTip { get; set; }
        /// <summary>Platos pedidos que aún no se han servido (no impiden ver la cuenta, sí cerrarla).</summary>
        public int PendingItems { get; set; }
    }

    public class BillLineDTO
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    /// <summary>Comanda para la pantalla de cocina: los platos pendientes de una cuenta en una zona.</summary>
    public class KitchenTicketDTO
    {
        public int OrderId { get; set; }
        public int TableNumber { get; set; }
        public string WaiterName { get; set; } = string.Empty;
        public int Guests { get; set; }
        public int StationId { get; set; }
        public string StationName { get; set; } = string.Empty;
        /// <summary>Hora del primer plato pendiente: lo que más tiempo lleva esperando.</summary>
        public DateTime FirstSentAt { get; set; }
        public List<KitchenTicketItemDTO> Items { get; set; } = new();
    }

    public class KitchenTicketItemDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Notes { get; set; }
        public OrderCourse Course { get; set; }
        public OrderItemStatus Status { get; set; }
        public DateTime? SentAt { get; set; }
    }
}

using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces;

namespace SistemaRestaurante.Domain.Entities
{
    /// <summary>Cuenta de una mesa: desde que se sientan los clientes hasta que pagan.</summary>
    public class Order : BaseEntity, IRestaurantScoped
    {
        public int RestaurantId { get; set; }

        public int TableId { get; set; }
        public Table Table { get; set; } = null!;

        /// <summary>Camarero que abrió la cuenta.</summary>
        public int WaiterId { get; set; }
        public User Waiter { get; set; } = null!;

        /// <summary>Reserva con la que llegaron los clientes, si la hubo.</summary>
        public int? ReservationId { get; set; }
        public Reservation? Reservation { get; set; }

        public int Guests { get; set; }
        public string? Notes { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Open;

        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
        public int? ClosedById { get; set; }

        /// <summary>Suma de lo servido al cerrar (sin propina).</summary>
        public decimal? Subtotal { get; set; }
        public decimal? TipAmount { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public string? CancelReason { get; set; }

        /// <summary>Control de concurrencia: dos dispositivos no pueden pisarse los cambios.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    /// <summary>Un plato (o bebida) pedido en una cuenta.</summary>
    public class OrderItem : BaseEntity, IRestaurantScoped
    {
        public int RestaurantId { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; } = null!;

        /// <summary>Zona que lo prepara (copiada del plato al pedirlo).</summary>
        public int StationId { get; set; }
        public Station Station { get; set; } = null!;

        /// <summary>Nombre y precio en el momento de pedirlo: si cambia la carta, la cuenta no cambia.</summary>
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? Notes { get; set; }
        public OrderCourse Course { get; set; }
        public OrderItemStatus Status { get; set; } = OrderItemStatus.Draft;

        /// <summary>
        /// Identificador que genera el dispositivo del camarero. Si la tablet reintenta el envío
        /// (Wi-Fi inestable), el plato no se duplica.
        /// </summary>
        public Guid? ClientRequestId { get; set; }

        public DateTime? SentAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? ReadyAt { get; set; }
        public DateTime? ServedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public int? CancelledById { get; set; }
        public string? CancelReason { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public decimal LineTotal => UnitPrice * Quantity;
    }
}

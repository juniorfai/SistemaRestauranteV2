using System.ComponentModel.DataAnnotations;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.API.DTOs.Request
{
    public class StationRequestDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre no puede superar 50 caracteres")]
        public string Name { get; set; } = string.Empty;

        [EnumDataType(typeof(StationType), ErrorMessage = "El tipo de zona no es válido")]
        public StationType Type { get; set; } = StationType.Kitchen;

        [Range(0, 999)]
        public int SortOrder { get; set; }

        /// <summary>Impresora térmica de la zona (IP:puerto), si la hay.</summary>
        [StringLength(100)]
        [RegularExpression(@"^[A-Za-z0-9.\-]+(:\d{1,5})?$",
            ErrorMessage = "La impresora debe indicarse como IP o nombre, con puerto opcional (p. ej. 192.168.1.50:9100)")]
        public string? PrinterAddress { get; set; }
    }

    public class OpenOrderRequestDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Indica la mesa")]
        public int TableId { get; set; }

        [Range(1, 50, ErrorMessage = "El número de comensales debe estar entre 1 y 50")]
        public int Guests { get; set; } = 1;

        /// <summary>Reserva con la que llegan, si la hay: se da por cumplida y sus platos se apuntan.</summary>
        public int? ReservationId { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class AddOrderItemDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Indica el plato")]
        public int MenuItemId { get; set; }

        [Range(1, 99, ErrorMessage = "La cantidad debe estar entre 1 y 99")]
        public int Quantity { get; set; } = 1;

        /// <summary>Indicaciones para cocina: "sin cebolla", "término medio"...</summary>
        [StringLength(200, ErrorMessage = "Las notas no pueden superar 200 caracteres")]
        public string? Notes { get; set; }

        /// <summary>Tiempo en que sale. Si no se indica, según la categoría del plato.</summary>
        [EnumDataType(typeof(OrderCourse))]
        public OrderCourse? Course { get; set; }

        /// <summary>Lo genera el dispositivo: si reintenta el envío, el plato no se duplica.</summary>
        public Guid? ClientRequestId { get; set; }
    }

    public class AddOrderItemsRequestDTO
    {
        [Required, MinLength(1, ErrorMessage = "Indica al menos un plato"), MaxLength(100)]
        public List<AddOrderItemDTO> Items { get; set; } = new();
    }

    public class UpdateOrderItemRequestDTO
    {
        [Range(1, 99, ErrorMessage = "La cantidad debe estar entre 1 y 99")]
        public int Quantity { get; set; } = 1;

        [StringLength(200, ErrorMessage = "Las notas no pueden superar 200 caracteres")]
        public string? Notes { get; set; }

        [EnumDataType(typeof(OrderCourse))]
        public OrderCourse Course { get; set; }
    }

    public class SendOrderRequestDTO
    {
        /// <summary>Retener los platos de este tiempo en adelante (p. ej. Main: los segundos esperan).</summary>
        [EnumDataType(typeof(OrderCourse))]
        public OrderCourse? HoldFromCourse { get; set; }
    }

    public class FireCourseRequestDTO
    {
        [Required(ErrorMessage = "Indica el tiempo a marchar")]
        [EnumDataType(typeof(OrderCourse))]
        public OrderCourse? Course { get; set; }
    }

    public class ItemStatusRequestDTO
    {
        [Required(ErrorMessage = "Indica el nuevo estado")]
        [EnumDataType(typeof(OrderItemStatus))]
        public OrderItemStatus? Status { get; set; }

        /// <summary>Estado que ve la pantalla; si otro dispositivo ya lo cambió, responde 409.</summary>
        [EnumDataType(typeof(OrderItemStatus))]
        public OrderItemStatus? ExpectedStatus { get; set; }
    }

    public class CancelRequestDTO
    {
        [Required(ErrorMessage = "Indica el motivo")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "El motivo debe tener entre 3 y 200 caracteres")]
        public string Reason { get; set; } = string.Empty;
    }

    public class MoveOrderRequestDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Indica la mesa")]
        public int TableId { get; set; }
    }

    public class GuestsRequestDTO
    {
        [Range(1, 50, ErrorMessage = "El número de comensales debe estar entre 1 y 50")]
        public int Guests { get; set; }
    }

    public class CloseOrderRequestDTO
    {
        [Required(ErrorMessage = "Indica la forma de pago")]
        [EnumDataType(typeof(PaymentMethod), ErrorMessage = "Forma de pago no válida")]
        public PaymentMethod? PaymentMethod { get; set; }

        /// <summary>Propina que deja el cliente (voluntaria; 0 si no deja).</summary>
        [Range(0, 100_000_000, ErrorMessage = "La propina no es válida")]
        public decimal TipAmount { get; set; }
    }
}

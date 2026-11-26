using System.ComponentModel.DataAnnotations;
using SistemaRestaurante.Domain.Constants;

namespace SistemaRestaurante.API.DTOs.Request
{
    public class ReservationRequestDTO
    {
        /// <summary>Fecha y hora de la reserva en la hora local del restaurante.</summary>
        [Required(ErrorMessage = "La fecha de la reserva es obligatoria")]
        public DateTime? ReservationDateTime { get; set; }

        [Range(1, ReservationRules.MaxPeoplePerReservation, ErrorMessage = "El número de personas no es válido")]
        public int NumberOfPeople { get; set; }

        [StringLength(500, ErrorMessage = "Las notas no pueden superar 500 caracteres")]
        public string? SpecialRequests { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El cliente es obligatorio")]
        public int CustomerId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La mesa es obligatoria")]
        public int TableId { get; set; }

        /// <summary>Platos pedidos por adelantado (opcional). Solo se usan al crear la reserva.</summary>
        public List<int> MenuItemIds { get; set; } = new List<int>();

        /// <summary>Cantidad de cada plato, en el mismo orden que MenuItemIds.</summary>
        public List<int> Quantities { get; set; } = new List<int>();
    }
}

using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.Domain.Entities
{
    public class Reservation : BaseEntity, IRestaurantScoped
    {
        public DateTime ReservationDateTime { get; set; }
        public int NumberOfPeople { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
        public string? SpecialRequests { get; set; }
        public decimal? TotalAmount { get; set; }

        // Foreign Keys
        public int CustomerId { get; set; }
        public int TableId { get; set; }
        /// <summary>Restaurante de la mesa; se guarda también aquí para filtrar rápido.</summary>
        public int RestaurantId { get; set; }

        // Navigation Properties
        public Customer Customer { get; set; } = null!;
        public Table Table { get; set; } = null!;
        public ICollection<ReservationMenuItem> ReservationMenuItems { get; set; } = new List<ReservationMenuItem>();
    }
}

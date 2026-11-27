using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.Domain.Entities
{
    public class Table : BaseEntity, IRestaurantScoped
    {
        public int TableNumber { get; set; }
        public int Capacity { get; set; }
        public string? Location { get; set; }
        public TableStatus Status { get; set; } = TableStatus.Available;

        // Foreign Keys
        public int RestaurantId { get; set; }

        // Navigation Properties
        public Restaurant Restaurant { get; set; } = null!;
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}

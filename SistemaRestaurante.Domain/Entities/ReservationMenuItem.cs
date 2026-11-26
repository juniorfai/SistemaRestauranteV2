using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.Domain.Entities
{
    public class ReservationMenuItem
    {
        public int ReservationId { get; set; }
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Navigation Properties
        public Reservation Reservation { get; set; } = null!;
        public MenuItem MenuItem { get; set; } = null!;
    }
}

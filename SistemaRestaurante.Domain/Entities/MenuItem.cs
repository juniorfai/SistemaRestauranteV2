using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.Domain.Entities
{
    public class MenuItem : BaseEntity, IRestaurantScoped
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public MenuItemCategory Category { get; set; }
        public bool IsAvailable { get; set; } = true;
        public Allergens Allergens { get; set; } = Allergens.None;

        // Foreign Keys
        public int RestaurantId { get; set; }

        /// <summary>Zona que lo prepara (cocina, barra...).</summary>
        public int StationId { get; set; }

        // Navigation Properties
        public Restaurant Restaurant { get; set; } = null!;
        public Station Station { get; set; } = null!;
        public ICollection<ReservationMenuItem> ReservationMenuItems { get; set; } = new List<ReservationMenuItem>();
    }
}

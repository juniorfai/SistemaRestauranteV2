using System;
using System.Collections.Generic;
using System.Text;

using SistemaRestaurante.Domain.Interfaces;

namespace SistemaRestaurante.Domain.Entities
{
    public class Customer : BaseEntity, IRestaurantScoped
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Preferences { get; set; }

        /// <summary>Cada restaurante tiene su propia lista de clientes.</summary>
        public int RestaurantId { get; set; }

        // Navigation Properties
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}

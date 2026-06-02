using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.Domain.Entities
{
    public class Restaurant : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? OpeningHours { get; set; }

        // Navigation Properties
        public ICollection<Table> Tables { get; set; } = new List<Table>();
        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}

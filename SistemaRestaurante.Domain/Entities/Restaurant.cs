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

        /// <summary>Moneda de la carta y las cuentas (ISO 4217).</summary>
        public string CurrencyCode { get; set; } = "COP";

        /// <summary>
        /// Propina sugerida en la cuenta, en %. En Colombia es voluntaria y debe indicarse así
        /// (Ley 1935 de 2018); 0 para no sugerir ninguna.
        /// </summary>
        public decimal SuggestedTipPercent { get; set; } = 10m;

        // Navigation Properties
        public ICollection<Table> Tables { get; set; } = new List<Table>();
        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}

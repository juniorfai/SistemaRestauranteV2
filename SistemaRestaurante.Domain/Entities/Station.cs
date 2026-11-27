using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces;

namespace SistemaRestaurante.Domain.Entities
{
    /// <summary>
    /// Zona de preparación (Cocina, Barra, Parrilla...). Cada plato va a una zona y cada zona
    /// tiene su pantalla y, opcionalmente, su impresora de comandas.
    /// </summary>
    public class Station : BaseEntity, IRestaurantScoped
    {
        public string Name { get; set; } = string.Empty;
        public StationType Type { get; set; } = StationType.Kitchen;
        public int SortOrder { get; set; }

        /// <summary>Impresora térmica de la zona (IP:puerto en la red del restaurante), si la hay.</summary>
        public string? PrinterAddress { get; set; }

        public int RestaurantId { get; set; }

        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}

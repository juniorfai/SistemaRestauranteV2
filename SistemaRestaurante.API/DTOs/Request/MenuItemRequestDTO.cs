using System.ComponentModel.DataAnnotations;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.API.DTOs.Request
{
    public class MenuItemRequestDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede superar 500 caracteres")]
        public string? Description { get; set; }

        [Range(0.01, 1_000_000, ErrorMessage = "El precio debe ser mayor que cero")]
        public decimal Price { get; set; }

        [EnumDataType(typeof(MenuItemCategory), ErrorMessage = "La categoría no es válida")]
        public MenuItemCategory Category { get; set; }

        public bool IsAvailable { get; set; } = true;

        /// <summary>Zona que lo prepara. Si no se indica: Barra para bebidas, Cocina para el resto.</summary>
        public int? StationId { get; set; }

        /// <summary>Alérgenos que contiene (nombres de <see cref="Allergens"/>, p. ej. "Gluten", "Milk").</summary>
        public List<Allergens>? Allergens { get; set; }
    }
}

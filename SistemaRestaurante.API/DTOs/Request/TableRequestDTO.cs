using System.ComponentModel.DataAnnotations;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.API.DTOs.Request
{
    public class TableRequestDTO
    {
        [Range(1, 9999, ErrorMessage = "El número de mesa no es válido")]
        public int TableNumber { get; set; }

        [Range(1, 50, ErrorMessage = "La capacidad debe estar entre 1 y 50")]
        public int Capacity { get; set; }

        [StringLength(100, ErrorMessage = "La ubicación no puede superar 100 caracteres")]
        public string? Location { get; set; }

        [EnumDataType(typeof(TableStatus), ErrorMessage = "El estado de la mesa no es válido")]
        public TableStatus Status { get; set; } = TableStatus.Available;

        [Range(1, int.MaxValue, ErrorMessage = "El restaurante es obligatorio")]
        public int RestaurantId { get; set; }
    }

    public class TableStatusRequestDTO
    {
        [Required]
        [EnumDataType(typeof(TableStatus), ErrorMessage = "El estado de la mesa no es válido")]
        public TableStatus? Status { get; set; }
    }
}

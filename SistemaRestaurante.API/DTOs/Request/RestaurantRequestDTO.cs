using System.ComponentModel.DataAnnotations;

namespace SistemaRestaurante.API.DTOs.Request
{
    public class RestaurantRequestDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\+?[0-9\s\-]{7,20}$", ErrorMessage = "El teléfono no es válido")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El email no es válido")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(200)]
        public string? OpeningHours { get; set; }
    }
}

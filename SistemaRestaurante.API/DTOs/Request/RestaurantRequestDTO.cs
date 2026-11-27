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

    /// <summary>Alta de restaurante: sus datos y los de su primer administrador.</summary>
    public class RestaurantCreateRequestDTO : RestaurantRequestDTO
    {
        [Required(ErrorMessage = "El usuario del administrador es obligatorio")]
        [RegularExpression(@"^[a-zA-Z0-9._-]{3,50}$",
            ErrorMessage = "El usuario debe tener de 3 a 50 caracteres: letras, números, punto, guion o guion bajo")]
        public string AdminUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del administrador es obligatorio")]
        [StringLength(100)]
        public string AdminFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña del administrador es obligatoria")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
        public string AdminPassword { get; set; } = string.Empty;
    }

    public class RestaurantActiveRequestDTO
    {
        [Required(ErrorMessage = "Indica si el restaurante está activo")]
        public bool? IsActive { get; set; }
    }
}

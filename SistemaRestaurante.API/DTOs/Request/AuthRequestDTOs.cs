using System.ComponentModel.DataAnnotations;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.API.DTOs.Request
{
    public class LoginRequestDTO
    {
        [Required(ErrorMessage = "El usuario es obligatorio")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordRequestDTO
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UserCreateRequestDTO
    {
        [Required(ErrorMessage = "El usuario es obligatorio")]
        [RegularExpression(@"^[a-zA-Z0-9._-]{3,50}$",
            ErrorMessage = "El usuario debe tener de 3 a 50 caracteres: letras, números, punto, guion o guion bajo")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio")]
        [EnumDataType(typeof(UserRole), ErrorMessage = "El rol no es válido")]
        public UserRole? Role { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
        public string Password { get; set; } = string.Empty;
    }

    public class UserUpdateRequestDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio")]
        [EnumDataType(typeof(UserRole), ErrorMessage = "El rol no es válido")]
        public UserRole? Role { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class ResetPasswordRequestDTO
    {
        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
        public string NewPassword { get; set; } = string.Empty;
    }
}

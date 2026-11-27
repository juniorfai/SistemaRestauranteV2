using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.Domain.Entities
{
    /// <summary>Usuario del personal que inicia sesión en la aplicación.</summary>
    public class User : BaseEntity
    {
        /// <summary>Nombre de acceso, en minúsculas y único.</summary>
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Staff;

        /// <summary>
        /// Cambia al modificar la contraseña, el rol o al desactivar al usuario;
        /// así los tokens emitidos antes dejan de valer.
        /// </summary>
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

        public DateTime? LastLoginAt { get; set; }

        /// <summary>Restaurante al que pertenece. Null solo para el administrador de la plataforma.</summary>
        public int? RestaurantId { get; set; }
        public Restaurant? Restaurant { get; set; }
    }
}

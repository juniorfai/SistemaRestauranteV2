using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.Domain.Interfaces.Services
{
    public interface IUserService
    {
        /// <summary>
        /// Devuelve el usuario si las credenciales son correctas y está activo; si no, null.
        /// Lanza ForbiddenAccessException si su restaurante está suspendido.
        /// </summary>
        Task<User?> ValidateCredentialsAsync(string username, string password);

        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);

        /// <summary>Alta de personal en el restaurante del usuario actual.</summary>
        Task<User> CreateAsync(string username, string fullName, UserRole role, string password);

        /// <summary>Alta de personal en un restaurante concreto (alta de restaurante y datos de ejemplo).</summary>
        Task<User> CreateForRestaurantAsync(int restaurantId, string username, string fullName, UserRole role, string password);

        /// <summary>Comprueba que el nombre de usuario está libre y la contraseña es válida, sin crear nada.</summary>
        Task EnsureCanCreateAsync(string username, string password);

        Task UpdateAsync(int id, string fullName, UserRole role, bool isActive, int currentUserId);
        Task ResetPasswordAsync(int id, string newPassword);
        Task ChangeOwnPasswordAsync(int id, string currentPassword, string newPassword);

        /// <summary>Crea el administrador de la plataforma si todavía no existe ninguno.</summary>
        Task<bool> EnsurePlatformAdminAsync(string username, string fullName, string password);
    }
}

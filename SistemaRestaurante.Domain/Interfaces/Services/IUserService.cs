using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.Domain.Interfaces.Services
{
    public interface IUserService
    {
        /// <summary>Devuelve el usuario si las credenciales son correctas y está activo; si no, null.</summary>
        Task<User?> ValidateCredentialsAsync(string username, string password);

        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User> CreateAsync(string username, string fullName, UserRole role, string password);
        Task UpdateAsync(int id, string fullName, UserRole role, bool isActive, int currentUserId);
        Task ResetPasswordAsync(int id, string newPassword);
        Task ChangeOwnPasswordAsync(int id, string currentPassword, string newPassword);

        /// <summary>Crea el primer administrador si todavía no existe ningún usuario.</summary>
        Task<bool> EnsureInitialAdminAsync(string username, string fullName, string password);
    }
}

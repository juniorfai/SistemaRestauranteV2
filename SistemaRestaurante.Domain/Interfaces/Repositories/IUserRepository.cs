using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.Domain.Interfaces.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        /// <summary>Busca por nombre de usuario, incluidos los desactivados (el nombre es único).</summary>
        Task<User?> GetByUsernameIncludingInactiveAsync(string username);

        /// <summary>Todos los usuarios, también los desactivados, para la pantalla de administración.</summary>
        Task<IEnumerable<User>> GetAllIncludingInactiveAsync();

        Task<User?> GetByIdIncludingInactiveAsync(int id);

        Task<int> CountActiveByRoleAsync(UserRole role);

        Task<bool> AnyAsync();
    }
}

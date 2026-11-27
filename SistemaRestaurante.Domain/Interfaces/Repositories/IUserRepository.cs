using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Usuarios. Las consultas normales solo ven los usuarios del restaurante actual
    /// (o todos, para el administrador de la plataforma).
    /// </summary>
    public interface IUserRepository : IGenericRepository<User>
    {
        /// <summary>Busca por nombre en todos los restaurantes (login y nombres únicos), con su restaurante.</summary>
        Task<User?> GetForLoginAsync(string username);

        /// <summary>Usuario de un token, en cualquier restaurante, con su restaurante.</summary>
        Task<User?> GetForAuthAsync(int id);

        /// <summary>Usuarios visibles, también los desactivados, con su restaurante.</summary>
        Task<IEnumerable<User>> GetAllIncludingInactiveAsync();

        Task<User?> GetByIdIncludingInactiveAsync(int id);

        Task<int> CountActiveByRoleAsync(UserRole role);

        Task<bool> AnyPlatformAdminAsync();
    }
}

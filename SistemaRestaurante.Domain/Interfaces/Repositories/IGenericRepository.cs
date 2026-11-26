using SistemaRestaurante.Domain.Entities;

namespace SistemaRestaurante.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Repositorio base. Solo devuelve registros activos y el borrado es lógico (IsActive = false).
    /// </summary>
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(int id);
        Task<T> CreateAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}

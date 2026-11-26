using Microsoft.EntityFrameworkCore;
using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Interfaces.Repositories;

namespace SistemaRestaurante.DataAccess.Repositories
{
    /// <summary>
    /// Repositorio base. Las lecturas solo devuelven registros activos y el borrado es lógico,
    /// para conservar el historial (reservas pasadas, platos pedidos, etc.).
    /// </summary>
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        protected readonly RestaurantDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(RestaurantDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        protected IQueryable<T> Active => _dbSet.Where(e => e.IsActive);

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await Active.ToListAsync();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await Active.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<T> CreateAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(T entity)
        {
            // Si la entidad ya está siendo rastreada, EF detecta los cambios solo; Update() marcaría
            // como modificadas también las entidades relacionadas cargadas.
            if (_context.Entry(entity).State == EntityState.Detached)
                _dbSet.Update(entity);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                entity.IsActive = false;
                entity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await Active.AnyAsync(e => e.Id == id);
        }
    }
}

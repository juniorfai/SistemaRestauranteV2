using Microsoft.EntityFrameworkCore;
using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Interfaces.Repositories;

namespace SistemaRestaurante.DataAccess.Repositories
{
    public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(RestaurantDbContext context) : base(context)
        {
        }

        public async Task<Customer?> GetByEmailAsync(string email)
        {
            return await Active.FirstOrDefaultAsync(c => c.Email == email);
        }

        public async Task<Customer?> GetByEmailIncludingInactiveAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.Email == email);
        }

        public async Task<IEnumerable<Customer>> GetCustomersWithReservationsAsync()
        {
            return await Active
                .Include(c => c.Reservations.Where(r => r.IsActive))
                .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerWithReservationsAsync(int id)
        {
            return await Active
                .Include(c => c.Reservations.Where(r => r.IsActive))
                .ThenInclude(r => r.Table)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}

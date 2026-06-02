using Microsoft.EntityFrameworkCore;
using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.DataAccess.Repositories
{
    public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(RestaurantDbContext context) : base(context)
        {
        }

        public async Task<Customer?> GetByEmailAsync(string email)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.Email == email);
        }

        public async Task<IEnumerable<Customer>> GetCustomersWithReservationsAsync()
        {
            return await _dbSet
                .Include(c => c.Reservations)
                .ThenInclude(r => r.Table)
                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerWithReservationsAsync(int id)
        {
            return await _dbSet
                .Include(c => c.Reservations)
                .ThenInclude(r => r.Table)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}

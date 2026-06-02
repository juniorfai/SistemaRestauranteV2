using SistemaRestaurante.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.Domain.Interfaces.Repositories
{
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        Task<Customer?> GetByEmailAsync(string email);
        Task<IEnumerable<Customer>> GetCustomersWithReservationsAsync();
        Task<Customer?> GetCustomerWithReservationsAsync(int id);
    }
}

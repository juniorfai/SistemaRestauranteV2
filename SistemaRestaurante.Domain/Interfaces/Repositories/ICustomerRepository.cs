using SistemaRestaurante.Domain.Entities;

namespace SistemaRestaurante.Domain.Interfaces.Repositories
{
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        /// <summary>Busca por email entre los clientes activos.</summary>
        Task<Customer?> GetByEmailAsync(string email);

        /// <summary>Busca por email incluyendo clientes dados de baja (el email es único en la base).</summary>
        Task<Customer?> GetByEmailIncludingInactiveAsync(string email);

        Task<IEnumerable<Customer>> GetCustomersWithReservationsAsync();
        Task<Customer?> GetCustomerWithReservationsAsync(int id);
    }
}

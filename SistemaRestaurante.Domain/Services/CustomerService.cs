using Microsoft.Extensions.Logging;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.Domain.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(
            ICustomerRepository customerRepository,
            IReservationRepository reservationRepository,
            ILogger<CustomerService> logger)
        {
            _customerRepository = customerRepository;
            _reservationRepository = reservationRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _customerRepository.GetCustomersWithReservationsAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            var customer = await _customerRepository.GetCustomerWithReservationsAsync(id);
            if (customer == null)
                _logger.LogWarning("Customer with ID {CustomerId} not found", id);

            return customer;
        }

        public async Task<Customer> CreateAsync(Customer customer)
        {
            customer.Email = NormalizeEmail(customer.Email);

            // El email es único en la base, también para clientes dados de baja.
            var existing = await _customerRepository.GetByEmailIncludingInactiveAsync(customer.Email);
            if (existing != null)
            {
                if (existing.IsActive)
                    throw new InvalidOperationException($"El email {customer.Email} ya está registrado");

                // Reactivar al cliente dado de baja con los datos nuevos.
                CopyData(customer, existing);
                existing.IsActive = true;
                existing.UpdatedAt = DateTime.UtcNow;
                await _customerRepository.UpdateAsync(existing);
                _logger.LogInformation("Reactivated customer {CustomerId}", existing.Id);
                return existing;
            }

            customer.Id = 0;
            customer.CreatedAt = DateTime.UtcNow;
            customer.IsActive = true;
            _logger.LogInformation("Creating customer {CustomerEmail}", customer.Email);
            return await _customerRepository.CreateAsync(customer);
        }

        public async Task UpdateAsync(int id, Customer customer)
        {
            var existing = await _customerRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró el cliente con ID {id}");

            customer.Email = NormalizeEmail(customer.Email);

            if (existing.Email != customer.Email)
            {
                var emailOwner = await _customerRepository.GetByEmailIncludingInactiveAsync(customer.Email);
                if (emailOwner != null && emailOwner.Id != id)
                    throw new InvalidOperationException($"El email {customer.Email} ya está registrado");
            }

            CopyData(customer, existing);
            existing.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Updating customer with ID: {CustomerId}", id);
            await _customerRepository.UpdateAsync(existing);
        }

        public async Task DeleteAsync(int id)
        {
            if (!await _customerRepository.ExistsAsync(id))
                throw new KeyNotFoundException($"No se encontró el cliente con ID {id}");

            if (await _reservationRepository.CustomerHasUpcomingReservationsAsync(id))
                throw new InvalidOperationException("El cliente tiene reservas próximas. Cancélalas antes de eliminarlo");

            _logger.LogInformation("Deleting customer with ID: {CustomerId}", id);
            await _customerRepository.DeleteAsync(id);
        }

        public async Task<Customer?> GetByEmailAsync(string email)
        {
            return await _customerRepository.GetByEmailAsync(NormalizeEmail(email));
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private static void CopyData(Customer source, Customer target)
        {
            target.FirstName = source.FirstName.Trim();
            target.LastName = source.LastName.Trim();
            target.Email = source.Email;
            target.PhoneNumber = source.PhoneNumber.Trim();
            target.Preferences = source.Preferences;
        }
    }
}

using Microsoft.Extensions.Logging;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using SistemaRestaurante.Domain.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.Domain.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(ICustomerRepository customerRepository, ILogger<CustomerService> logger)
        {
            _customerRepository = customerRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            _logger.LogInformation("Retrieving all customers");
            return await _customerRepository.GetCustomersWithReservationsAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Retrieving customer with ID: {CustomerId}", id);
            var customer = await _customerRepository.GetCustomerWithReservationsAsync(id);

            if (customer == null)
                _logger.LogWarning("Customer with ID {CustomerId} not found", id);

            return customer;
        }

        public async Task<Customer> CreateAsync(Customer customer)
        {
            // Validar email único
            var existingCustomer = await _customerRepository.GetByEmailAsync(customer.Email);
            if (existingCustomer != null)
            {
                _logger.LogWarning("Email {Email} already exists", customer.Email);
                throw new InvalidOperationException($"El email {customer.Email} ya está registrado");
            }

            _logger.LogInformation("Creating customer: {FirstName} {LastName}", customer.FirstName, customer.LastName);
            return await _customerRepository.CreateAsync(customer);
        }

        public async Task UpdateAsync(int id, Customer customer)
        {
            var existingCustomer = await _customerRepository.GetByIdAsync(id);
            if (existingCustomer == null)
            {
                throw new KeyNotFoundException($"No se encontró el cliente con ID {id}");
            }

            // Validar email único si cambió
            if (existingCustomer.Email != customer.Email)
            {
                var emailExists = await _customerRepository.GetByEmailAsync(customer.Email);
                if (emailExists != null)
                {
                    throw new InvalidOperationException($"El email {customer.Email} ya está registrado");
                }
            }

            existingCustomer.FirstName = customer.FirstName;
            existingCustomer.LastName = customer.LastName;
            existingCustomer.Email = customer.Email;
            existingCustomer.PhoneNumber = customer.PhoneNumber;
            existingCustomer.Preferences = customer.Preferences;
            existingCustomer.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Updating customer with ID: {CustomerId}", id);
            await _customerRepository.UpdateAsync(existingCustomer);
        }

        public async Task DeleteAsync(int id)
        {
            var exists = await _customerRepository.ExistsAsync(id);
            if (!exists)
            {
                throw new KeyNotFoundException($"No se encontró el cliente con ID {id}");
            }

            _logger.LogInformation("Deleting customer with ID: {CustomerId}", id);
            await _customerRepository.DeleteAsync(id);
        }

        public async Task<Customer?> GetByEmailAsync(string email)
        {
            return await _customerRepository.GetByEmailAsync(email);
        }
    }
}

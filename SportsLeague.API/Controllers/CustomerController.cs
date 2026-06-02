using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SistemaRestaurante.API.DTOs.Request;
using SistemaRestaurante.API.DTOs.Response;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IMapper _mapper;

        public CustomerController(ICustomerService customerService, IMapper mapper)
        {
            _customerService = customerService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerResponseDTO>>> GetAll()
        {
            var customers = await _customerService.GetAllAsync();
            var customersDto = _mapper.Map<IEnumerable<CustomerResponseDTO>>(customers);
            return Ok(customersDto);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerResponseDTO>> GetById(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null)
                return NotFound(new { message = $"Cliente con ID {id} no encontrado" });

            var customerDto = _mapper.Map<CustomerResponseDTO>(customer);
            return Ok(customerDto);
        }

        [HttpGet("email/{email}")]
        public async Task<ActionResult<CustomerResponseDTO>> GetByEmail(string email)
        {
            var customer = await _customerService.GetByEmailAsync(email);
            if (customer == null)
                return NotFound(new { message = $"Cliente con email {email} no encontrado" });

            var customerDto = _mapper.Map<CustomerResponseDTO>(customer);
            return Ok(customerDto);
        }

        [HttpPost]
        public async Task<ActionResult<CustomerResponseDTO>> Create(CustomerRequestDTO dto)
        {
            try
            {
                var customer = _mapper.Map<Customer>(dto);
                var created = await _customerService.CreateAsync(customer);
                var responseDto = _mapper.Map<CustomerResponseDTO>(created);

                return CreatedAtAction(nameof(GetById), new { id = responseDto.Id }, responseDto);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, CustomerRequestDTO dto)
        {
            try
            {
                var customer = _mapper.Map<Customer>(dto);
                await _customerService.UpdateAsync(id, customer);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                await _customerService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaRestaurante.API.DTOs.Request;
using SistemaRestaurante.API.DTOs.Response;
using SistemaRestaurante.Domain.Constants;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
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
            return Ok(_mapper.Map<IEnumerable<CustomerResponseDTO>>(customers));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CustomerResponseDTO>> GetById(int id)
        {
            var customer = await _customerService.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró el cliente con ID {id}");
            return Ok(_mapper.Map<CustomerResponseDTO>(customer));
        }

        [HttpGet("email/{email}")]
        public async Task<ActionResult<CustomerResponseDTO>> GetByEmail(string email)
        {
            var customer = await _customerService.GetByEmailAsync(email)
                ?? throw new KeyNotFoundException("No se encontró un cliente con ese email");
            return Ok(_mapper.Map<CustomerResponseDTO>(customer));
        }

        [HttpPost]
        public async Task<ActionResult<CustomerResponseDTO>> Create(CustomerRequestDTO dto)
        {
            var created = await _customerService.CreateAsync(_mapper.Map<Customer>(dto));
            var response = _mapper.Map<CustomerResponseDTO>(created);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, CustomerRequestDTO dto)
        {
            await _customerService.UpdateAsync(id, _mapper.Map<Customer>(dto));
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Management)]
        public async Task<IActionResult> Delete(int id)
        {
            await _customerService.DeleteAsync(id);
            return NoContent();
        }
    }
}

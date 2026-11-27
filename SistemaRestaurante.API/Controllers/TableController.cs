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
    [Authorize(Roles = Roles.RestaurantStaff)]
    public class TableController : ControllerBase
    {
        private readonly ITableService _tableService;
        private readonly IMapper _mapper;

        public TableController(ITableService tableService, IMapper mapper)
        {
            _tableService = tableService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TableResponseDTO>>> GetAll()
        {
            var tables = await _tableService.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<TableResponseDTO>>(tables));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TableResponseDTO>> GetById(int id)
        {
            var table = await _tableService.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró la mesa con ID {id}");
            return Ok(_mapper.Map<TableResponseDTO>(table));
        }

        /// <summary>Mesas libres para una fecha/hora y un número de personas.</summary>
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<TableResponseDTO>>> GetAvailable(
            [FromQuery] DateTime dateTime,
            [FromQuery] int numberOfPeople)
        {
            var tables = await _tableService.GetAvailableAsync(dateTime, numberOfPeople);
            return Ok(_mapper.Map<IEnumerable<TableResponseDTO>>(tables));
        }

        [HttpGet("restaurant/{restaurantId:int}")]
        public async Task<ActionResult<IEnumerable<TableResponseDTO>>> GetByRestaurant(int restaurantId)
        {
            var tables = await _tableService.GetByRestaurantAsync(restaurantId);
            return Ok(_mapper.Map<IEnumerable<TableResponseDTO>>(tables));
        }

        [HttpPost]
        [Authorize(Roles = Roles.Management)]
        public async Task<ActionResult<TableResponseDTO>> Create(TableRequestDTO dto)
        {
            var created = await _tableService.CreateAsync(_mapper.Map<Table>(dto));
            var table = await _tableService.GetByIdAsync(created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.Map<TableResponseDTO>(table));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = Roles.Management)]
        public async Task<IActionResult> Update(int id, TableRequestDTO dto)
        {
            await _tableService.UpdateAsync(id, _mapper.Map<Table>(dto));
            return NoContent();
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, TableStatusRequestDTO dto)
        {
            await _tableService.UpdateStatusAsync(id, dto.Status!.Value);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Management)]
        public async Task<IActionResult> Delete(int id)
        {
            await _tableService.DeleteAsync(id);
            return NoContent();
        }
    }
}

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
    /// <summary>Zonas de preparación (Cocina, Barra...). Las ve todo el personal; las gestiona el encargado.</summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Roles = Roles.AllRestaurantRoles)]
    public class StationController : ControllerBase
    {
        private readonly IStationService _stationService;
        private readonly IMapper _mapper;

        public StationController(IStationService stationService, IMapper mapper)
        {
            _stationService = stationService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StationResponseDTO>>> GetAll() =>
            Ok(_mapper.Map<IEnumerable<StationResponseDTO>>(await _stationService.GetAllAsync()));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<StationResponseDTO>> GetById(int id)
        {
            var station = await _stationService.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró la zona con ID {id}");
            return Ok(_mapper.Map<StationResponseDTO>(station));
        }

        [HttpPost]
        [Authorize(Roles = Roles.Management)]
        public async Task<ActionResult<StationResponseDTO>> Create(StationRequestDTO dto)
        {
            var created = await _stationService.CreateAsync(_mapper.Map<Station>(dto));
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.Map<StationResponseDTO>(created));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = Roles.Management)]
        public async Task<IActionResult> Update(int id, StationRequestDTO dto)
        {
            await _stationService.UpdateAsync(id, _mapper.Map<Station>(dto));
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Management)]
        public async Task<IActionResult> Delete(int id)
        {
            await _stationService.DeleteAsync(id);
            return NoContent();
        }
    }
}

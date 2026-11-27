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
    /// <summary>
    /// Restaurantes. El administrador de la plataforma los da de alta y los suspende;
    /// el administrador de cada restaurante edita los datos del suyo.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class RestaurantController : ControllerBase
    {
        private const string PlatformOrRestaurantAdmin = Roles.PlatformAdmin + "," + Roles.Admin;

        private readonly IRestaurantService _restaurantService;
        private readonly IMapper _mapper;

        public RestaurantController(IRestaurantService restaurantService, IMapper mapper)
        {
            _restaurantService = restaurantService;
            _mapper = mapper;
        }

        /// <summary>Todos los restaurantes, también los suspendidos.</summary>
        [HttpGet]
        [Authorize(Roles = Roles.PlatformAdmin)]
        public async Task<ActionResult<IEnumerable<RestaurantResponseDTO>>> GetAll()
        {
            var restaurants = await _restaurantService.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<RestaurantResponseDTO>>(restaurants));
        }

        /// <summary>Restaurante del usuario conectado.</summary>
        [HttpGet("current")]
        [Authorize(Roles = Roles.RestaurantStaff)]
        public async Task<ActionResult<RestaurantResponseDTO>> GetCurrent()
        {
            return Ok(_mapper.Map<RestaurantResponseDTO>(await _restaurantService.GetCurrentAsync()));
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = PlatformOrRestaurantAdmin)]
        public async Task<ActionResult<RestaurantResponseDTO>> GetById(int id)
        {
            var restaurant = await _restaurantService.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró el restaurante con ID {id}");
            return Ok(_mapper.Map<RestaurantResponseDTO>(restaurant));
        }

        /// <summary>Alta de un restaurante junto con su primer administrador.</summary>
        [HttpPost]
        [Authorize(Roles = Roles.PlatformAdmin)]
        public async Task<ActionResult<RestaurantResponseDTO>> Create(RestaurantCreateRequestDTO dto)
        {
            var created = await _restaurantService.CreateWithAdminAsync(
                _mapper.Map<Restaurant>(dto), dto.AdminUsername, dto.AdminFullName, dto.AdminPassword);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.Map<RestaurantResponseDTO>(created));
        }

        /// <summary>Datos del restaurante. Un administrador de restaurante solo puede editar el suyo.</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = PlatformOrRestaurantAdmin)]
        public async Task<IActionResult> Update(int id, RestaurantRequestDTO dto)
        {
            await _restaurantService.UpdateAsync(id, _mapper.Map<Restaurant>(dto));
            return NoContent();
        }

        /// <summary>Suspende (isActive = false) o reactiva un restaurante.</summary>
        [HttpPatch("{id:int}/active")]
        [Authorize(Roles = Roles.PlatformAdmin)]
        public async Task<IActionResult> SetActive(int id, RestaurantActiveRequestDTO dto)
        {
            await _restaurantService.SetActiveAsync(id, dto.IsActive!.Value);
            return NoContent();
        }
    }
}

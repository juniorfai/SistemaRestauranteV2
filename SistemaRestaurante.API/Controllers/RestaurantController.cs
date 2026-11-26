using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SistemaRestaurante.API.DTOs.Request;
using SistemaRestaurante.API.DTOs.Response;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class RestaurantController : ControllerBase
    {
        private readonly IRestaurantService _restaurantService;
        private readonly IMapper _mapper;

        public RestaurantController(IRestaurantService restaurantService, IMapper mapper)
        {
            _restaurantService = restaurantService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RestaurantResponseDTO>>> GetAll()
        {
            var restaurants = await _restaurantService.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<RestaurantResponseDTO>>(restaurants));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<RestaurantResponseDTO>> GetById(int id)
        {
            var restaurant = await _restaurantService.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró el restaurante con ID {id}");
            return Ok(_mapper.Map<RestaurantResponseDTO>(restaurant));
        }

        [HttpPost]
        public async Task<ActionResult<RestaurantResponseDTO>> Create(RestaurantRequestDTO dto)
        {
            var created = await _restaurantService.CreateAsync(_mapper.Map<Restaurant>(dto));
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.Map<RestaurantResponseDTO>(created));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, RestaurantRequestDTO dto)
        {
            await _restaurantService.UpdateAsync(id, _mapper.Map<Restaurant>(dto));
            return NoContent();
        }
    }
}

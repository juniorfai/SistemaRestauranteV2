using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaRestaurante.API.DTOs.Request;
using SistemaRestaurante.API.DTOs.Response;
using SistemaRestaurante.Domain.Constants;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Roles = Roles.RestaurantStaff)]
    public class MenuItemController : ControllerBase
    {
        private readonly IMenuItemService _menuItemService;
        private readonly IMapper _mapper;

        public MenuItemController(IMenuItemService menuItemService, IMapper mapper)
        {
            _menuItemService = menuItemService;
            _mapper = mapper;
        }

        /// <summary>Platos del menú. Por defecto solo los disponibles; includeUnavailable=true los trae todos.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenuItemResponseDTO>>> GetAll([FromQuery] bool includeUnavailable = false)
        {
            var items = await _menuItemService.GetAllAsync(includeUnavailable);
            return Ok(_mapper.Map<IEnumerable<MenuItemResponseDTO>>(items));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MenuItemResponseDTO>> GetById(int id)
        {
            var item = await _menuItemService.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró el plato con ID {id}");
            return Ok(_mapper.Map<MenuItemResponseDTO>(item));
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<MenuItemResponseDTO>>> GetByCategory(MenuItemCategory category)
        {
            var items = await _menuItemService.GetByCategoryAsync(category);
            return Ok(_mapper.Map<IEnumerable<MenuItemResponseDTO>>(items));
        }

        [HttpGet("restaurant/{restaurantId:int}")]
        public async Task<ActionResult<IEnumerable<MenuItemResponseDTO>>> GetByRestaurant(int restaurantId)
        {
            var items = await _menuItemService.GetByRestaurantAsync(restaurantId);
            return Ok(_mapper.Map<IEnumerable<MenuItemResponseDTO>>(items));
        }

        [HttpPost]
        [Authorize(Roles = Roles.Management)]
        public async Task<ActionResult<MenuItemResponseDTO>> Create(MenuItemRequestDTO dto)
        {
            var created = await _menuItemService.CreateAsync(_mapper.Map<MenuItem>(dto));
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.Map<MenuItemResponseDTO>(created));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = Roles.Management)]
        public async Task<IActionResult> Update(int id, MenuItemRequestDTO dto)
        {
            await _menuItemService.UpdateAsync(id, _mapper.Map<MenuItem>(dto));
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Management)]
        public async Task<IActionResult> Delete(int id)
        {
            await _menuItemService.DeleteAsync(id);
            return NoContent();
        }
    }
}

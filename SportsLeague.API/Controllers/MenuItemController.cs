using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SistemaRestaurante.API.DTOs.Response;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MenuItemController : ControllerBase
    {
        private readonly IMenuItemRepository _menuItemRepository;
        private readonly IMapper _mapper;

        public MenuItemController(IMenuItemRepository menuItemRepository, IMapper mapper)
        {
            _menuItemRepository = menuItemRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenuItemResponseDTO>>> GetAll()
        {
            var items = await _menuItemRepository.GetAvailableItemsAsync();
            var itemsDto = _mapper.Map<IEnumerable<MenuItemResponseDTO>>(items);
            return Ok(itemsDto);
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<MenuItemResponseDTO>>> GetByCategory(MenuItemCategory category)
        {
            var items = await _menuItemRepository.GetByCategoryAsync(category);
            var itemsDto = _mapper.Map<IEnumerable<MenuItemResponseDTO>>(items);
            return Ok(itemsDto);
        }

        [HttpGet("restaurant/{restaurantId}")]
        public async Task<ActionResult<IEnumerable<MenuItemResponseDTO>>> GetByRestaurant(int restaurantId)
        {
            var items = await _menuItemRepository.GetByRestaurantAsync(restaurantId);
            var itemsDto = _mapper.Map<IEnumerable<MenuItemResponseDTO>>(items);
            return Ok(itemsDto);
        }
    }
}

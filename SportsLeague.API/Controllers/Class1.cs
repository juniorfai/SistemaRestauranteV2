using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SistemaRestaurante.API.DTOs.Response;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TableController : ControllerBase
    {
        private readonly ITableRepository _tableRepository;
        private readonly IMapper _mapper;

        public TableController(ITableRepository tableRepository, IMapper mapper)
        {
            _tableRepository = tableRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TableResponseDTO>>> GetAll()
        {
            var tables = await _tableRepository.GetAllAsync();
            var tablesDto = _mapper.Map<IEnumerable<TableResponseDTO>>(tables);
            return Ok(tablesDto);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TableResponseDTO>> GetById(int id)
        {
            var table = await _tableRepository.GetByIdAsync(id);
            if (table == null)
                return NotFound(new { message = $"Mesa con ID {id} no encontrada" });

            var tableDto = _mapper.Map<TableResponseDTO>(table);
            return Ok(tableDto);
        }

        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<TableResponseDTO>>> GetAvailable(
            [FromQuery] DateTime dateTime,
            [FromQuery] int numberOfPeople)
        {
            var tables = await _tableRepository.GetAvailableTablesAsync(dateTime, numberOfPeople);
            var tablesDto = _mapper.Map<IEnumerable<TableResponseDTO>>(tables);
            return Ok(tablesDto);
        }

        [HttpGet("restaurant/{restaurantId}")]
        public async Task<ActionResult<IEnumerable<TableResponseDTO>>> GetByRestaurant(int restaurantId)
        {
            var tables = await _tableRepository.GetTablesByRestaurantAsync(restaurantId);
            var tablesDto = _mapper.Map<IEnumerable<TableResponseDTO>>(tables);
            return Ok(tablesDto);
        }
    }
}

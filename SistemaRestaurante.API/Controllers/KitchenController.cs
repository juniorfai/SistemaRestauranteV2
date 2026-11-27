using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaRestaurante.API.DTOs.Request;
using SistemaRestaurante.API.DTOs.Response;
using SistemaRestaurante.API.Mappings;
using SistemaRestaurante.Domain.Constants;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.API.Controllers
{
    /// <summary>
    /// Pantalla de cocina y barra: comandas pendientes por zona. La usa el rol Cocina y también
    /// el personal de sala (p. ej. el camarero que prepara las bebidas en la barra).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Roles = Roles.AllRestaurantRoles)]
    public class KitchenController : ControllerBase
    {
        /// <summary>Lo que cocina puede marcar; servir en la mesa es cosa de sala.</summary>
        private static readonly OrderItemStatus[] KitchenStatuses =
            { OrderItemStatus.Sent, OrderItemStatus.Preparing, OrderItemStatus.Ready };

        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;

        public KitchenController(IOrderService orderService, IMapper mapper)
        {
            _orderService = orderService;
            _mapper = mapper;
        }

        /// <summary>Comandas pendientes (una por cuenta y zona), la más antigua primero.</summary>
        [HttpGet("tickets")]
        public async Task<ActionResult<IEnumerable<KitchenTicketDTO>>> GetTickets([FromQuery] int? stationId) =>
            Ok(AutoMapperProfile.ToTickets(await _orderService.GetKitchenItemsAsync(stationId)));

        /// <summary>Cambia el estado de un plato: en preparación, listo (o deshacer).</summary>
        [HttpPut("orders/{orderId:int}/items/{itemId:int}/status")]
        public async Task<ActionResult<OrderItemResponseDTO>> ChangeItemStatus(int orderId, int itemId, ItemStatusRequestDTO dto)
        {
            if (!KitchenStatuses.Contains(dto.Status!.Value))
                throw new ArgumentException("Desde cocina un plato solo puede pasar a enviado, en preparación o listo");

            var item = await _orderService.ChangeItemStatusAsync(orderId, itemId, dto.Status.Value, dto.ExpectedStatus);
            return Ok(_mapper.Map<OrderItemResponseDTO>(item));
        }

        /// <summary>Despacha la comanda de una zona: todos sus platos pendientes quedan listos.</summary>
        [HttpPost("orders/{orderId:int}/stations/{stationId:int}/bump")]
        public async Task<IActionResult> Bump(int orderId, int stationId)
        {
            await _orderService.BumpAsync(orderId, stationId);
            return NoContent();
        }
    }
}

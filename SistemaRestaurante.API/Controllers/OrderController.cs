using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SistemaRestaurante.API.Auth;
using SistemaRestaurante.API.DTOs.Request;
using SistemaRestaurante.API.DTOs.Response;
using SistemaRestaurante.API.Mappings;
using SistemaRestaurante.Domain.Constants;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.API.Controllers
{
    /// <summary>
    /// Cuentas de las mesas (servicio en sala). Las usa el personal de sala; anular platos
    /// enviados, anular cuentas y ver el histórico es del encargado o el administrador.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Roles = Roles.RestaurantStaff)]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;

        public OrderController(IOrderService orderService, IMapper mapper)
        {
            _orderService = orderService;
            _mapper = mapper;
        }

        /// <summary>Cuentas abiertas ahora mismo (el plano de sala).</summary>
        [HttpGet("open")]
        public async Task<ActionResult<IEnumerable<OrderResponseDTO>>> GetOpen() =>
            Ok(_mapper.Map<IEnumerable<OrderResponseDTO>>(await _orderService.GetOpenAsync()));

        /// <summary>Cuentas cerradas o anuladas en un día (hora local del restaurante).</summary>
        [HttpGet("history")]
        [Authorize(Roles = Roles.Management)]
        public async Task<ActionResult<IEnumerable<OrderResponseDTO>>> GetHistory([FromQuery] DateOnly date) =>
            Ok(_mapper.Map<IEnumerable<OrderResponseDTO>>(await _orderService.GetClosedOnAsync(date)));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderResponseDTO>> GetById(int id) =>
            Ok(_mapper.Map<OrderResponseDTO>(await _orderService.GetAsync(id)));

        /// <summary>Sentar a los clientes: abre la cuenta de la mesa.</summary>
        [HttpPost]
        public async Task<ActionResult<OrderResponseDTO>> Open(OpenOrderRequestDTO dto)
        {
            var order = await _orderService.OpenAsync(dto.TableId, dto.Guests, dto.ReservationId, dto.Notes, User.GetUserId());
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, _mapper.Map<OrderResponseDTO>(order));
        }

        /// <summary>Apunta platos (quedan como borrador hasta enviarlos a cocina).</summary>
        [HttpPost("{id:int}/items")]
        public async Task<ActionResult<OrderResponseDTO>> AddItems(int id, AddOrderItemsRequestDTO dto)
        {
            var items = dto.Items
                .Select(i => new NewOrderItem(i.MenuItemId, i.Quantity, i.Notes, i.Course, i.ClientRequestId))
                .ToList();
            return Ok(_mapper.Map<OrderResponseDTO>(await _orderService.AddItemsAsync(id, items)));
        }

        [HttpPut("{id:int}/items/{itemId:int}")]
        public async Task<IActionResult> UpdateItem(int id, int itemId, UpdateOrderItemRequestDTO dto)
        {
            await _orderService.UpdateDraftItemAsync(id, itemId, dto.Quantity, dto.Notes, dto.Course);
            return NoContent();
        }

        /// <summary>Quita un plato que aún no se ha enviado a cocina.</summary>
        [HttpDelete("{id:int}/items/{itemId:int}")]
        public async Task<IActionResult> RemoveItem(int id, int itemId)
        {
            await _orderService.RemoveDraftItemAsync(id, itemId);
            return NoContent();
        }

        /// <summary>Anula un plato ya enviado (queda registrado quién y por qué).</summary>
        [HttpPost("{id:int}/items/{itemId:int}/cancel")]
        [Authorize(Roles = Roles.Management)]
        public async Task<IActionResult> CancelItem(int id, int itemId, CancelRequestDTO dto)
        {
            await _orderService.CancelItemAsync(id, itemId, dto.Reason, User.GetUserId());
            return NoContent();
        }

        /// <summary>Marca un plato como servido (o deshace un cambio de estado).</summary>
        [HttpPut("{id:int}/items/{itemId:int}/status")]
        public async Task<ActionResult<OrderItemResponseDTO>> ChangeItemStatus(int id, int itemId, ItemStatusRequestDTO dto)
        {
            var item = await _orderService.ChangeItemStatusAsync(id, itemId, dto.Status!.Value, dto.ExpectedStatus);
            return Ok(_mapper.Map<OrderItemResponseDTO>(item));
        }

        /// <summary>Envía a cocina lo apuntado; opcionalmente retiene los tiempos siguientes.</summary>
        [HttpPost("{id:int}/send")]
        public async Task<ActionResult<OrderResponseDTO>> Send(int id,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SendOrderRequestDTO? dto) =>
            Ok(_mapper.Map<OrderResponseDTO>(await _orderService.SendAsync(id, dto?.HoldFromCourse)));

        /// <summary>"Marchar": libera los platos retenidos hasta ese tiempo.</summary>
        [HttpPost("{id:int}/fire")]
        public async Task<ActionResult<OrderResponseDTO>> Fire(int id, FireCourseRequestDTO dto) =>
            Ok(_mapper.Map<OrderResponseDTO>(await _orderService.FireAsync(id, dto.Course!.Value)));

        [HttpPut("{id:int}/table")]
        public async Task<IActionResult> Move(int id, MoveOrderRequestDTO dto)
        {
            await _orderService.MoveToTableAsync(id, dto.TableId);
            return NoContent();
        }

        [HttpPut("{id:int}/guests")]
        public async Task<IActionResult> SetGuests(int id, GuestsRequestDTO dto)
        {
            await _orderService.SetGuestsAsync(id, dto.Guests);
            return NoContent();
        }

        /// <summary>Cuenta provisional para el cliente, con la propina sugerida (voluntaria).</summary>
        [HttpGet("{id:int}/bill")]
        public async Task<ActionResult<BillResponseDTO>> GetBill(int id) =>
            Ok(AutoMapperProfile.ToDto(await _orderService.GetBillAsync(id)));

        /// <summary>Cobrada: cierra la cuenta y libera la mesa.</summary>
        [HttpPost("{id:int}/close")]
        public async Task<ActionResult<OrderResponseDTO>> Close(int id, CloseOrderRequestDTO dto)
        {
            var order = await _orderService.CloseAsync(id, dto.PaymentMethod!.Value, dto.TipAmount, User.GetUserId());
            return Ok(_mapper.Map<OrderResponseDTO>(order));
        }

        /// <summary>Anula la cuenta entera (clientes que se van, error al abrir...).</summary>
        [HttpPost("{id:int}/cancel")]
        [Authorize(Roles = Roles.Management)]
        public async Task<IActionResult> Cancel(int id, CancelRequestDTO dto)
        {
            await _orderService.CancelAsync(id, dto.Reason, User.GetUserId());
            return NoContent();
        }
    }
}

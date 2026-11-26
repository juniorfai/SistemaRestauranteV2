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
    public class ReservationController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly IMapper _mapper;

        public ReservationController(IReservationService reservationService, IMapper mapper)
        {
            _reservationService = reservationService;
            _mapper = mapper;
        }

        /// <summary>Reservas pendientes o confirmadas desde hoy en adelante.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservationResponseDTO>>> GetAll()
        {
            var reservations = await _reservationService.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<ReservationResponseDTO>>(reservations));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ReservationResponseDTO>> GetById(int id)
        {
            var reservation = await _reservationService.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró la reserva con ID {id}");
            return Ok(_mapper.Map<ReservationResponseDTO>(reservation));
        }

        /// <summary>Todas las reservas de un día (cualquier estado). Formato de fecha: yyyy-MM-dd.</summary>
        [HttpGet("date/{date:datetime}")]
        public async Task<ActionResult<IEnumerable<ReservationResponseDTO>>> GetByDate(DateTime date)
        {
            var reservations = await _reservationService.GetByDateAsync(date);
            return Ok(_mapper.Map<IEnumerable<ReservationResponseDTO>>(reservations));
        }

        [HttpGet("customer/{customerId:int}")]
        public async Task<ActionResult<IEnumerable<ReservationResponseDTO>>> GetByCustomer(int customerId)
        {
            var reservations = await _reservationService.GetByCustomerAsync(customerId);
            return Ok(_mapper.Map<IEnumerable<ReservationResponseDTO>>(reservations));
        }

        [HttpPost]
        public async Task<ActionResult<ReservationResponseDTO>> Create(ReservationRequestDTO dto)
        {
            var reservation = _mapper.Map<Reservation>(dto);
            var created = await _reservationService.CreateAsync(reservation, dto.MenuItemIds, dto.Quantities);
            var response = await LoadResponseAsync(created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
        }

        /// <summary>Cambia fecha, mesa, personas o notas. Los platos pedidos no se modifican aquí.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ReservationResponseDTO>> Update(int id, ReservationRequestDTO dto)
        {
            var reservation = _mapper.Map<Reservation>(dto);
            await _reservationService.UpdateAsync(id, reservation);
            return Ok(await LoadResponseAsync(id));
        }

        [HttpPatch("{id:int}/confirm")]
        public async Task<IActionResult> Confirm(int id)
        {
            await _reservationService.ConfirmReservationAsync(id);
            return NoContent();
        }

        [HttpPatch("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            await _reservationService.CancelReservationAsync(id);
            return NoContent();
        }

        [HttpPatch("{id:int}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            await _reservationService.CompleteReservationAsync(id);
            return NoContent();
        }

        [HttpPatch("{id:int}/no-show")]
        public async Task<IActionResult> NoShow(int id)
        {
            await _reservationService.MarkNoShowAsync(id);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Management)]
        public async Task<IActionResult> Delete(int id)
        {
            await _reservationService.DeleteAsync(id);
            return NoContent();
        }

        private async Task<ReservationResponseDTO> LoadResponseAsync(int id)
        {
            var reservation = await _reservationService.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró la reserva con ID {id}");
            return _mapper.Map<ReservationResponseDTO>(reservation);
        }
    }
}

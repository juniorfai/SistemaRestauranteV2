using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaRestaurante.API.Auth;
using SistemaRestaurante.API.DTOs.Request;
using SistemaRestaurante.API.DTOs.Response;
using SistemaRestaurante.Domain.Constants;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.API.Controllers
{
    /// <summary>Gestión de usuarios del personal. Solo administradores.</summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Roles = Roles.Admin)]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public UserController(IUserService userService, IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDTO>>> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<UserResponseDTO>>(users));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserResponseDTO>> GetById(int id)
        {
            var user = await _userService.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró el usuario con ID {id}");
            return Ok(_mapper.Map<UserResponseDTO>(user));
        }

        [HttpPost]
        public async Task<ActionResult<UserResponseDTO>> Create(UserCreateRequestDTO dto)
        {
            var user = await _userService.CreateAsync(dto.Username, dto.FullName, dto.Role!.Value, dto.Password);
            var response = _mapper.Map<UserResponseDTO>(user);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        /// <summary>Cambia nombre, rol o estado. Para dar de baja, enviar isActive = false.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UserUpdateRequestDTO dto)
        {
            await _userService.UpdateAsync(id, dto.FullName, dto.Role!.Value, dto.IsActive, User.GetUserId());
            return NoContent();
        }

        [HttpPost("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequestDTO dto)
        {
            await _userService.ResetPasswordAsync(id, dto.NewPassword);
            return NoContent();
        }
    }
}

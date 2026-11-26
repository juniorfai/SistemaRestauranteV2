using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SistemaRestaurante.API.Auth;
using SistemaRestaurante.API.DTOs.Request;
using SistemaRestaurante.API.DTOs.Response;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        public const string LoginRateLimitPolicy = "login";

        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public AuthController(IUserService userService, ITokenService tokenService, IMapper mapper)
        {
            _userService = userService;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        /// <summary>Inicia sesión y devuelve el token para la cabecera Authorization: Bearer.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(LoginRateLimitPolicy)]
        public async Task<ActionResult<LoginResponseDTO>> Login(LoginRequestDTO dto)
        {
            var user = await _userService.ValidateCredentialsAsync(dto.Username, dto.Password)
                ?? throw new UnauthorizedAccessException("Usuario o contraseña incorrectos");

            var (token, expiresAt) = _tokenService.CreateToken(user);
            return Ok(new LoginResponseDTO
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = _mapper.Map<UserResponseDTO>(user)
            });
        }

        /// <summary>Datos del usuario de la sesión actual.</summary>
        [HttpGet("me")]
        public async Task<ActionResult<UserResponseDTO>> Me()
        {
            var user = await _userService.GetByIdAsync(User.GetUserId())
                ?? throw new UnauthorizedAccessException("Sesión no válida");
            return Ok(_mapper.Map<UserResponseDTO>(user));
        }

        /// <summary>Cambia la contraseña propia. Cierra las demás sesiones del usuario.</summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequestDTO dto)
        {
            await _userService.ChangeOwnPasswordAsync(User.GetUserId(), dto.CurrentPassword, dto.NewPassword);
            return NoContent();
        }
    }
}

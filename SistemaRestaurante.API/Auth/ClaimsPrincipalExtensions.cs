using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SistemaRestaurante.API.Auth
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>Id del usuario autenticado (claim "sub").</summary>
        public static int GetUserId(this ClaimsPrincipal principal)
        {
            var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, out var id)
                ? id
                : throw new UnauthorizedAccessException("Sesión no válida");
        }
    }
}

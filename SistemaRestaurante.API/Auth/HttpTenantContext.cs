using System.Security.Claims;
using SistemaRestaurante.Domain.Constants;
using SistemaRestaurante.Domain.Interfaces;

namespace SistemaRestaurante.API.Auth
{
    /// <summary>Lee el restaurante y el rol del token de la petición actual.</summary>
    public class HttpTenantContext : ITenantContext
    {
        private readonly IHttpContextAccessor _accessor;

        public HttpTenantContext(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        private ClaimsPrincipal? User => _accessor.HttpContext?.User;

        public int? RestaurantId =>
            int.TryParse(User?.FindFirstValue(TokenService.RestaurantClaim), out var id) ? id : null;

        public bool IsPlatformAdmin => User?.IsInRole(Roles.PlatformAdmin) ?? false;
    }
}

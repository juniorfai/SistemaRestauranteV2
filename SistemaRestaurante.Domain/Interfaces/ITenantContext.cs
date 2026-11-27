namespace SistemaRestaurante.Domain.Interfaces
{
    /// <summary>
    /// Restaurante del usuario que hace la petición. Toda la información de un restaurante
    /// (mesas, carta, clientes, reservas y personal) queda aislada de la de los demás.
    /// </summary>
    public interface ITenantContext
    {
        /// <summary>Restaurante del usuario; null para el administrador de la plataforma o fuera de una petición.</summary>
        int? RestaurantId { get; }

        /// <summary>Administrador de la plataforma: gestiona restaurantes, no su día a día.</summary>
        bool IsPlatformAdmin { get; }
    }

    public static class TenantContextExtensions
    {
        /// <summary>Restaurante actual; falla si la petición no pertenece a un restaurante.</summary>
        public static int RequireRestaurantId(this ITenantContext tenant) =>
            tenant.RestaurantId ?? throw new ForbiddenAccessException("Esta acción requiere un usuario de un restaurante");
    }

    /// <summary>El usuario tiene sesión pero no permiso para esta acción (HTTP 403).</summary>
    public class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException(string message) : base(message) { }
    }

    /// <summary>Entidad que pertenece a un restaurante.</summary>
    public interface IRestaurantScoped
    {
        int RestaurantId { get; set; }
    }
}

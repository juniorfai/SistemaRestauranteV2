namespace SistemaRestaurante.Domain.Constants
{
    /// <summary>Nombres de rol para [Authorize(Roles = ...)]. Coinciden con <see cref="Enums.UserRole"/>.</summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Staff = "Staff";

        /// <summary>Admin y encargado: altas, bajas y cambios de mesas, carta y clientes.</summary>
        public const string Management = Admin + "," + Manager;
    }
}

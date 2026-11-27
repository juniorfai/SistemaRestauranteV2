namespace SistemaRestaurante.Domain.Constants
{
    /// <summary>Nombres de rol para [Authorize(Roles = ...)]. Coinciden con <see cref="Enums.UserRole"/>.</summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Staff = "Staff";
        public const string PlatformAdmin = "PlatformAdmin";
        public const string Kitchen = "Kitchen";

        /// <summary>Admin y encargado: altas, bajas y cambios de mesas, carta y clientes.</summary>
        public const string Management = Admin + "," + Manager;

        /// <summary>Cualquier usuario de un restaurante (no el administrador de la plataforma).</summary>
        public const string RestaurantStaff = Admin + "," + Manager + "," + Staff;

        /// <summary>Todo el personal del restaurante, incluida la cocina.</summary>
        public const string AllRestaurantRoles = RestaurantStaff + "," + Kitchen;
    }
}

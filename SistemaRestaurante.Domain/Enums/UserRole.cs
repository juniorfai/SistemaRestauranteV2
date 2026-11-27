namespace SistemaRestaurante.Domain.Enums
{
    /// <summary>
    /// Roles del personal. Admin: todo, incluidos usuarios y datos del restaurante.
    /// Manager (encargado): gestiona reservas, mesas, clientes y carta.
    /// Staff (camarero/recepción): trabaja con reservas, clientes y estado de las mesas.
    /// PlatformAdmin: quien vende el sistema; da de alta y suspende restaurantes. No pertenece a ninguno.
    /// Kitchen (cocina): solo la pantalla de comandas de cocina/barra.
    /// </summary>
    public enum UserRole
    {
        Admin = 0,
        Manager = 1,
        Staff = 2,
        PlatformAdmin = 3,
        Kitchen = 4
    }
}

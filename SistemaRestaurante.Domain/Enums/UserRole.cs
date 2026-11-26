namespace SistemaRestaurante.Domain.Enums
{
    /// <summary>
    /// Roles del personal. Admin: todo, incluidos usuarios y datos del restaurante.
    /// Manager (encargado): gestiona reservas, mesas, clientes y carta.
    /// Staff (camarero/recepción): trabaja con reservas, clientes y estado de las mesas.
    /// </summary>
    public enum UserRole
    {
        Admin = 0,
        Manager = 1,
        Staff = 2
    }
}

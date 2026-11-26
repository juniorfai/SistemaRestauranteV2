namespace SistemaRestaurante.Domain.Constants
{
    /// <summary>
    /// Reglas de negocio de las reservas. Las fechas se manejan en la hora local del restaurante.
    /// </summary>
    public static class ReservationRules
    {
        /// <summary>Tiempo que una reserva ocupa la mesa.</summary>
        public static readonly TimeSpan Duration = TimeSpan.FromHours(2);

        public const int MaxPeoplePerReservation = 50;
    }
}

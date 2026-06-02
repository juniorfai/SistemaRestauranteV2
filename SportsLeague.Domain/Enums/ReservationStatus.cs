using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.Domain.Enums
{
    public enum ReservationStatus
    {
        Pending = 0,
        Confirmed = 1,
        Completed = 2,
        Cancelled = 3,
        NoShow = 4
    }
}

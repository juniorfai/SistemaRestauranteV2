using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.API.DTOs.Request
{
    public class ReservationRequestDTO
    {
        public DateTime ReservationDateTime { get; set; }
        public int NumberOfPeople { get; set; }
        public string? SpecialRequests { get; set; }
        public int CustomerId { get; set; }
        public int TableId { get; set; }
        public List<int> MenuItemIds { get; set; } = new List<int>();
        public List<int> Quantities { get; set; } = new List<int>();
    }
}

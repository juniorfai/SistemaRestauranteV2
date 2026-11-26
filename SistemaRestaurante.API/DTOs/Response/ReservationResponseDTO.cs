using SistemaRestaurante.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.API.DTOs.Response
{
    public class ReservationResponseDTO
    {
        public int Id { get; set; }
        public DateTime ReservationDateTime { get; set; }
        public int NumberOfPeople { get; set; }
        public ReservationStatus Status { get; set; }
        public string? SpecialRequests { get; set; }
        public decimal? TotalAmount { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public int TableId { get; set; }
        public int TableNumber { get; set; }
        public int TableCapacity { get; set; }
        public List<MenuItemOrderDTO> OrderedItems { get; set; } = new List<MenuItemOrderDTO>();
        public DateTime CreatedAt { get; set; }
    }

    public class MenuItemOrderDTO
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }
}

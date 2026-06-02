using System;
using System.Collections.Generic;
using System.Text;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.API.DTOs.Request
{
    public class TableRequestDTO
    {
        public int TableNumber { get; set; }
        public int Capacity { get; set; }
        public string? Location { get; set; }
        public TableStatus Status { get; set; }
        public int RestaurantId { get; set; }
    }
}

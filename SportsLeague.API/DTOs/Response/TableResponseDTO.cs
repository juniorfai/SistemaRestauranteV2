using System;
using System.Collections.Generic;
using System.Text;
using SistemaRestaurante.Domain.Enums;

namespace SistemaRestaurante.API.DTOs.Response
{
    public class TableResponseDTO
    {
        public int Id { get; set; }
        public int TableNumber { get; set; }
        public int Capacity { get; set; }
        public string? Location { get; set; }
        public TableStatus Status { get; set; }
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
    }
}

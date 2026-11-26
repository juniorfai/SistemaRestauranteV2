using SistemaRestaurante.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.API.DTOs.Response
{
    public class MenuItemResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public MenuItemCategory Category { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public int RestaurantId { get; set; }
    }
}

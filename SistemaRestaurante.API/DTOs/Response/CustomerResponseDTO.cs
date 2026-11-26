using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.API.DTOs.Response
{
    public class CustomerResponseDTO
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Preferences { get; set; }
        public int TotalReservations { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

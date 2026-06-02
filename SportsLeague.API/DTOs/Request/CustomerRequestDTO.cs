using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.API.DTOs.Request
{
    public class CustomerRequestDTO
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Preferences { get; set; }
    }
}

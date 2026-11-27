namespace SistemaRestaurante.Tests.Infrastructure
{
    /// <summary>Un restaurante recién creado, con la sesión de su administrador y atajos para llenarlo.</summary>
    public class TestRestaurant
    {
        private readonly ApiFactory _factory;
        private int _nextTableNumber = 1;

        public int Id { get; }
        public string Name { get; }
        public string AdminUsername { get; }
        public string AdminPassword { get; }
        public ApiClient Admin { get; }

        public TestRestaurant(ApiFactory factory, int id, string name, string adminUsername, string adminPassword, ApiClient admin)
        {
            _factory = factory;
            Id = id;
            Name = name;
            AdminUsername = adminUsername;
            AdminPassword = adminPassword;
            Admin = admin;
        }

        public Task<TableDto> AddTableAsync(int capacity = 4, string status = "Available") =>
            Admin.PostOkAsync<TableDto>("/api/Table", new
            {
                tableNumber = _nextTableNumber++,
                capacity,
                location = "Sala",
                status,
            });

        public Task<CustomerDto> AddCustomerAsync(string? email = null)
        {
            var suffix = Guid.NewGuid().ToString("N")[..6];
            return Admin.PostOkAsync<CustomerDto>("/api/Customer", new
            {
                firstName = "Cliente",
                lastName = suffix,
                email = email ?? $"c{suffix}@pruebas.test",
                phoneNumber = "611223344",
            });
        }

        public Task<MenuItemDto> AddMenuItemAsync(decimal price = 10m, bool isAvailable = true) =>
            Admin.PostOkAsync<MenuItemDto>("/api/MenuItem", new
            {
                name = $"Plato {Guid.NewGuid().ToString("N")[..6]}",
                description = "De prueba",
                price,
                category = "MainCourse",
                isAvailable,
            });

        /// <summary>Da de alta a alguien del personal y devuelve su sesión.</summary>
        public async Task<ApiClient> AddStaffAsync(string role)
        {
            var username = $"{role.ToLowerInvariant()}{Guid.NewGuid().ToString("N")[..6]}";
            const string password = "Personal-Test-1!";
            await Admin.PostOkAsync<UserDto>("/api/User", new { username, fullName = $"{role} de pruebas", role, password });
            return await _factory.LoginAsync(username, password);
        }

        public Task<ReservationDto> ReserveAsync(int tableId, int customerId, DateTime when, int people = 2) =>
            Admin.PostOkAsync<ReservationDto>("/api/Reservation", Reservation(tableId, customerId, when, people));

        public static object Reservation(int tableId, int customerId, DateTime when, int people = 2,
            int[]? menuItemIds = null, int[]? quantities = null) => new
        {
            reservationDateTime = when.ToString("yyyy-MM-ddTHH:mm:ss"),
            numberOfPeople = people,
            customerId,
            tableId,
            menuItemIds = menuItemIds ?? Array.Empty<int>(),
            quantities = quantities ?? Array.Empty<int>(),
        };

        /// <summary>Una fecha futura a las 20:00 (hora local, como guarda la API).</summary>
        public static DateTime FutureEvening(int daysAhead = 5) => DateTime.Today.AddDays(daysAhead).AddHours(20);
    }
}

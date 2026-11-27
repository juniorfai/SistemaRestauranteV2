using System.Net;
using SistemaRestaurante.Tests.Infrastructure;

namespace SistemaRestaurante.Tests
{
    /// <summary>Qué puede hacer cada rol dentro de su restaurante.</summary>
    public class PermissionTests : IClassFixture<ApiFactory>, IAsyncLifetime
    {
        private readonly ApiFactory _api;
        private TestRestaurant _restaurant = null!;
        private ApiClient _manager = null!;
        private ApiClient _waiter = null!;
        private TableDto _table = null!;
        private CustomerDto _customer = null!;
        private MenuItemDto _dish = null!;

        public PermissionTests(ApiFactory api)
        {
            _api = api;
        }

        public async Task InitializeAsync()
        {
            _restaurant = await _api.CreateRestaurantAsync();
            _manager = await _restaurant.AddStaffAsync("Manager");
            _waiter = await _restaurant.AddStaffAsync("Staff");
            _table = await _restaurant.AddTableAsync();
            _customer = await _restaurant.AddCustomerAsync();
            _dish = await _restaurant.AddMenuItemAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private static object NewTable(int number) => new { tableNumber = number, capacity = 2, location = "Terraza", status = "Available" };

        [Fact]
        public async Task Waiter_handles_reservations_customers_and_table_status()
        {
            var customer = await _waiter.PostOkAsync<CustomerDto>("/api/Customer", new
            {
                firstName = "Nuevo", lastName = "Cliente", email = "nuevo.camarero@pruebas.test", phoneNumber = "633445566",
            });
            var reservation = await _waiter.PostOkAsync<ReservationDto>("/api/Reservation",
                TestRestaurant.Reservation(_table.Id, customer.Id, TestRestaurant.FutureEvening(10)));

            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, _waiter.PatchAsync($"/api/Reservation/{reservation.Id}/confirm"));
            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, _waiter.PatchAsync($"/api/Table/{_table.Id}/status", new { status = "Occupied" }));
            await HttpAssert.StatusAsync(HttpStatusCode.OK, _waiter.GetAsync("/api/MenuItem"));
        }

        [Fact]
        public async Task Waiter_cannot_change_tables_menu_or_delete_things()
        {
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _waiter.PostAsync("/api/Table", NewTable(50)));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _waiter.PutAsync($"/api/Table/{_table.Id}", NewTable(51)));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _waiter.DeleteAsync($"/api/Table/{_table.Id}"));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _waiter.DeleteAsync($"/api/Customer/{_customer.Id}"));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _waiter.DeleteAsync($"/api/MenuItem/{_dish.Id}"));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _waiter.PostAsync("/api/MenuItem",
                new { name = "Nuevo", price = 5, category = "Dessert", isAvailable = true }));
        }

        [Fact]
        public async Task Manager_manages_tables_and_menu_but_not_staff_or_restaurant()
        {
            await HttpAssert.StatusAsync(HttpStatusCode.Created, _manager.PostAsync("/api/Table", NewTable(60)));
            await HttpAssert.StatusAsync(HttpStatusCode.Created, _manager.PostAsync("/api/MenuItem",
                new { name = "Tarta", price = 5.5, category = "Dessert", isAvailable = true }));

            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _manager.GetAsync("/api/User"));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _manager.PostAsync("/api/User",
                new { username = "intruso", fullName = "X", role = "Admin", password = "Intruso-123!" }));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _manager.PutAsync($"/api/Restaurant/{_restaurant.Id}", new
            {
                name = "Renombrado", address = "C/ 1", phoneNumber = "600000000", email = "x@y.test",
            }));
        }

        [Fact]
        public async Task Everyone_in_the_restaurant_can_read_its_details()
        {
            foreach (var client in new[] { _restaurant.Admin, _manager, _waiter })
            {
                var current = await client.GetOkAsync<RestaurantDto>("/api/Restaurant/current");
                Assert.Equal(_restaurant.Id, current.Id);
            }
        }

        [Fact]
        public async Task Restaurant_staff_cannot_use_platform_endpoints()
        {
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _restaurant.Admin.GetAsync("/api/Restaurant"));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _restaurant.Admin.PatchAsync($"/api/Restaurant/{_restaurant.Id}/active",
                new { isActive = false }));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _restaurant.Admin.PostAsync("/api/Restaurant", new
            {
                name = "Pirata", address = "C/ 1", phoneNumber = "600000000", email = "p@y.test",
                adminUsername = "pirata", adminFullName = "Pirata", adminPassword = "Pirata-1234!",
            }));
        }

        [Fact]
        public async Task Admin_cannot_create_a_platform_admin()
        {
            var status = await _restaurant.Admin.StatusOfAsync(_restaurant.Admin.PostAsync("/api/User",
                new { username = "superusuario", fullName = "X", role = "PlatformAdmin", password = "Super-1234!" }));
            Assert.True(status is HttpStatusCode.BadRequest or HttpStatusCode.Conflict, $"Llegó {(int)status}");
        }

        [Fact]
        public async Task Admin_edits_own_restaurant_details()
        {
            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, _restaurant.Admin.PutAsync($"/api/Restaurant/{_restaurant.Id}", new
            {
                name = "Nombre nuevo", address = "Calle nueva 2", phoneNumber = "+34 600 000 001", email = "NUEVO@Pruebas.test", openingHours = "13-16",
            }));

            var current = await _restaurant.Admin.GetOkAsync<RestaurantDto>("/api/Restaurant/current");
            Assert.Equal("Nombre nuevo", current.Name);
            Assert.Equal("nuevo@pruebas.test", current.Email);
        }
    }
}

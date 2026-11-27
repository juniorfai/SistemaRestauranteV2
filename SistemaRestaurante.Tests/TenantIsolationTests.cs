using System.Net;
using SistemaRestaurante.Tests.Infrastructure;

namespace SistemaRestaurante.Tests
{
    /// <summary>Cada restaurante solo ve y toca sus propios datos.</summary>
    public class TenantIsolationTests : IClassFixture<ApiFactory>, IAsyncLifetime
    {
        private readonly ApiFactory _api;
        private TestRestaurant _mine = null!;
        private TestRestaurant _other = null!;
        private TableDto _otherTable = null!;
        private CustomerDto _otherCustomer = null!;
        private MenuItemDto _otherDish = null!;
        private ReservationDto _otherReservation = null!;

        public TenantIsolationTests(ApiFactory api)
        {
            _api = api;
        }

        public async Task InitializeAsync()
        {
            _mine = await _api.CreateRestaurantAsync();
            _other = await _api.CreateRestaurantAsync();
            _otherTable = await _other.AddTableAsync();
            _otherCustomer = await _other.AddCustomerAsync();
            _otherDish = await _other.AddMenuItemAsync();
            _otherReservation = await _other.ReserveAsync(_otherTable.Id, _otherCustomer.Id, TestRestaurant.FutureEvening());
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task New_restaurant_starts_empty()
        {
            Assert.Empty(await _mine.Admin.GetOkAsync<List<TableDto>>("/api/Table"));
            Assert.Empty(await _mine.Admin.GetOkAsync<List<CustomerDto>>("/api/Customer"));
            Assert.Empty(await _mine.Admin.GetOkAsync<List<MenuItemDto>>("/api/MenuItem?includeUnavailable=true"));
            Assert.Empty(await _mine.Admin.GetOkAsync<List<ReservationDto>>("/api/Reservation"));
        }

        [Fact]
        public async Task Lists_only_show_own_data()
        {
            var myTable = await _mine.AddTableAsync();

            var tables = await _mine.Admin.GetOkAsync<List<TableDto>>("/api/Table");
            Assert.Equal(new[] { myTable.Id }, tables.Select(t => t.Id));
            Assert.All(tables, t => Assert.Equal(_mine.Id, t.RestaurantId));
        }

        [Fact]
        public async Task Staff_list_only_shows_own_restaurant()
        {
            var users = await _mine.Admin.GetOkAsync<List<UserDto>>("/api/User");
            Assert.Equal(new[] { _mine.AdminUsername }, users.Select(u => u.Username));
        }

        [Fact]
        public async Task Reading_another_restaurants_records_returns_404()
        {
            var me = _mine.Admin;
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, me.GetAsync($"/api/Table/{_otherTable.Id}"));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, me.GetAsync($"/api/Customer/{_otherCustomer.Id}"));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, me.GetAsync($"/api/Customer/email/{_otherCustomer.Email}"));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, me.GetAsync($"/api/MenuItem/{_otherDish.Id}"));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, me.GetAsync($"/api/Reservation/{_otherReservation.Id}"));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, me.GetAsync($"/api/Restaurant/{_other.Id}"));
        }

        [Fact]
        public async Task Filtering_by_another_restaurant_id_returns_nothing()
        {
            var tables = await _mine.Admin.GetOkAsync<List<TableDto>>($"/api/Table/restaurant/{_other.Id}");
            var dishes = await _mine.Admin.GetOkAsync<List<MenuItemDto>>($"/api/MenuItem/restaurant/{_other.Id}");
            Assert.Empty(tables);
            Assert.Empty(dishes);
        }

        [Fact]
        public async Task Changing_another_restaurants_records_returns_404_and_changes_nothing()
        {
            var me = _mine.Admin;
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, me.PutAsync($"/api/Table/{_otherTable.Id}",
                new { tableNumber = 99, capacity = 2, location = "x", status = "Available" }));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, me.PatchAsync($"/api/Table/{_otherTable.Id}/status", new { status = "OutOfService" }));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, me.DeleteAsync($"/api/Table/{_otherTable.Id}"));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, me.PutAsync($"/api/Customer/{_otherCustomer.Id}",
                new { firstName = "X", lastName = "Y", email = "x@y.test", phoneNumber = "611223344" }));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, me.DeleteAsync($"/api/Customer/{_otherCustomer.Id}"));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, me.DeleteAsync($"/api/MenuItem/{_otherDish.Id}"));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, me.PatchAsync($"/api/Reservation/{_otherReservation.Id}/cancel"));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, me.DeleteAsync($"/api/Reservation/{_otherReservation.Id}"));

            var table = await _other.Admin.GetOkAsync<TableDto>($"/api/Table/{_otherTable.Id}");
            var reservation = await _other.Admin.GetOkAsync<ReservationDto>($"/api/Reservation/{_otherReservation.Id}");
            Assert.Equal(_otherTable.TableNumber, table.TableNumber);
            Assert.Equal("Available", table.Status);
            Assert.Equal("Pending", reservation.Status);
            await HttpAssert.StatusAsync(HttpStatusCode.OK, _other.Admin.GetAsync($"/api/Customer/{_otherCustomer.Id}"));
        }

        [Fact]
        public async Task Cannot_manage_another_restaurants_staff()
        {
            var otherAdmin = _other.Admin.Session!.User;
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, _mine.Admin.GetAsync($"/api/User/{otherAdmin.Id}"));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, _mine.Admin.PutAsync($"/api/User/{otherAdmin.Id}",
                new { fullName = "Hackeado", role = "Staff", isActive = false }));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, _mine.Admin.PostAsync($"/api/User/{otherAdmin.Id}/reset-password",
                new { newPassword = "Hackeo-1234!" }));

            await _api.LoginAsync(_other.AdminUsername, _other.AdminPassword);
        }

        [Fact]
        public async Task Cannot_book_with_another_restaurants_table_customer_or_dish()
        {
            var myTable = await _mine.AddTableAsync();
            var myCustomer = await _mine.AddCustomerAsync();
            var when = TestRestaurant.FutureEvening(8);

            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, _mine.Admin.PostAsync("/api/Reservation",
                TestRestaurant.Reservation(_otherTable.Id, myCustomer.Id, when)));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, _mine.Admin.PostAsync("/api/Reservation",
                TestRestaurant.Reservation(myTable.Id, _otherCustomer.Id, when)));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, _mine.Admin.PostAsync("/api/Reservation",
                TestRestaurant.Reservation(myTable.Id, myCustomer.Id, when, menuItemIds: new[] { _otherDish.Id }, quantities: new[] { 1 })));
        }

        [Fact]
        public async Task Same_customer_email_and_table_number_can_exist_in_two_restaurants()
        {
            var customer = await _mine.AddCustomerAsync(_otherCustomer.Email);
            var table = await _mine.AddTableAsync();

            Assert.Equal(_otherCustomer.Email, customer.Email);
            Assert.Equal(_otherTable.TableNumber, table.TableNumber);
        }

        [Fact]
        public async Task Customer_email_is_unique_within_a_restaurant()
        {
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, _other.Admin.PostAsync("/api/Customer", new
            {
                firstName = "Otra",
                lastName = "Persona",
                email = _otherCustomer.Email,
                phoneNumber = "622334455",
            }));
        }
    }
}

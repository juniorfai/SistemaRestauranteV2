using System.Net;
using SistemaRestaurante.Tests.Infrastructure;

namespace SistemaRestaurante.Tests
{
    /// <summary>Reglas de negocio de las reservas: horarios, capacidad, estados y pedido anticipado.</summary>
    public class ReservationRulesTests : IClassFixture<ApiFactory>, IAsyncLifetime
    {
        private readonly ApiFactory _api;
        private TestRestaurant _r = null!;
        private CustomerDto _customer = null!;

        public ReservationRulesTests(ApiFactory api)
        {
            _api = api;
        }

        public async Task InitializeAsync()
        {
            _r = await _api.CreateRestaurantAsync();
            _customer = await _r.AddCustomerAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private Task<HttpResponseMessage> Reserve(int tableId, DateTime when, int people = 2) =>
            _r.Admin.PostAsync("/api/Reservation", TestRestaurant.Reservation(tableId, _customer.Id, when, people));

        [Fact]
        public async Task Table_cannot_be_booked_twice_within_two_hours()
        {
            var table = await _r.AddTableAsync();
            var when = TestRestaurant.FutureEvening();
            await _r.ReserveAsync(table.Id, _customer.Id, when);

            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, Reserve(table.Id, when));
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, Reserve(table.Id, when.AddMinutes(90)));
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, Reserve(table.Id, when.AddMinutes(-90)));
        }

        [Fact]
        public async Task Table_can_be_booked_again_two_hours_later()
        {
            var table = await _r.AddTableAsync();
            var when = TestRestaurant.FutureEvening();
            await _r.ReserveAsync(table.Id, _customer.Id, when);

            await HttpAssert.StatusAsync(HttpStatusCode.Created, Reserve(table.Id, when.AddHours(2)));
            await HttpAssert.StatusAsync(HttpStatusCode.Created, Reserve(table.Id, when.AddHours(-2)));
        }

        [Fact]
        public async Task Another_table_is_free_at_the_same_time()
        {
            var first = await _r.AddTableAsync();
            var second = await _r.AddTableAsync();
            var when = TestRestaurant.FutureEvening();
            await _r.ReserveAsync(first.Id, _customer.Id, when);

            await HttpAssert.StatusAsync(HttpStatusCode.Created, Reserve(second.Id, when));
        }

        [Fact]
        public async Task Cancelling_frees_the_table()
        {
            var table = await _r.AddTableAsync();
            var when = TestRestaurant.FutureEvening();
            var reservation = await _r.ReserveAsync(table.Id, _customer.Id, when);

            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, _r.Admin.PatchAsync($"/api/Reservation/{reservation.Id}/cancel"));
            await HttpAssert.StatusAsync(HttpStatusCode.Created, Reserve(table.Id, when));
        }

        [Fact]
        public async Task Simultaneous_requests_for_the_same_slot_produce_a_single_booking()
        {
            var table = await _r.AddTableAsync();
            var when = TestRestaurant.FutureEvening(20);

            var attempts = Enumerable.Range(0, 8).Select(async _ =>
            {
                using var response = await Reserve(table.Id, when);
                return response.StatusCode;
            });
            var results = await Task.WhenAll(attempts);

            Assert.Equal(1, results.Count(s => s == HttpStatusCode.Created));
            Assert.All(results.Where(s => s != HttpStatusCode.Created), s => Assert.Equal(HttpStatusCode.Conflict, s));

            var bookings = await _r.Admin.GetOkAsync<List<ReservationDto>>($"/api/Reservation/date/{when:yyyy-MM-dd}");
            Assert.Single(bookings, b => b.TableId == table.Id);
        }

        [Fact]
        public async Task Available_tables_exclude_booked_small_and_out_of_service_ones()
        {
            var booked = await _r.AddTableAsync(capacity: 4);
            var small = await _r.AddTableAsync(capacity: 2);
            var broken = await _r.AddTableAsync(capacity: 6, status: "OutOfService");
            var free = await _r.AddTableAsync(capacity: 6);
            var when = TestRestaurant.FutureEvening(12);
            await _r.ReserveAsync(booked.Id, _customer.Id, when);

            var available = await _r.Admin.GetOkAsync<List<TableDto>>(
                $"/api/Table/available?dateTime={when:yyyy-MM-ddTHH:mm}&numberOfPeople=4");

            var ids = available.Select(t => t.Id).ToList();
            Assert.Contains(free.Id, ids);
            Assert.DoesNotContain(booked.Id, ids);
            Assert.DoesNotContain(small.Id, ids);
            Assert.DoesNotContain(broken.Id, ids);
        }

        [Fact]
        public async Task Party_larger_than_the_table_is_rejected()
        {
            var table = await _r.AddTableAsync(capacity: 2);
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, Reserve(table.Id, TestRestaurant.FutureEvening(), people: 3));
        }

        [Fact]
        public async Task Out_of_service_table_is_rejected()
        {
            var table = await _r.AddTableAsync(status: "OutOfService");
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, Reserve(table.Id, TestRestaurant.FutureEvening()));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(51)]
        public async Task Number_of_people_must_be_reasonable(int people)
        {
            var table = await _r.AddTableAsync(capacity: 50);
            await HttpAssert.StatusAsync(HttpStatusCode.BadRequest, Reserve(table.Id, TestRestaurant.FutureEvening(), people));
        }

        [Fact]
        public async Task Past_dates_are_rejected()
        {
            var table = await _r.AddTableAsync();
            await HttpAssert.StatusAsync(HttpStatusCode.BadRequest, Reserve(table.Id, DateTime.Now.AddHours(-1)));
        }

        [Fact]
        public async Task Status_changes_follow_the_allowed_order()
        {
            var table = await _r.AddTableAsync();
            var reservation = await _r.ReserveAsync(table.Id, _customer.Id, TestRestaurant.FutureEvening(15));
            var url = $"/api/Reservation/{reservation.Id}";

            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, _r.Admin.PatchAsync($"{url}/confirm"));
            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, _r.Admin.PatchAsync($"{url}/complete"));
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, _r.Admin.PatchAsync($"{url}/cancel"));
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, _r.Admin.PatchAsync($"{url}/confirm"));

            var final = await _r.Admin.GetOkAsync<ReservationDto>(url);
            Assert.Equal("Completed", final.Status);
        }

        [Fact]
        public async Task Pre_order_total_is_calculated_from_menu_prices()
        {
            var table = await _r.AddTableAsync();
            var starter = await _r.AddMenuItemAsync(price: 6.50m);
            var main = await _r.AddMenuItemAsync(price: 18.25m);

            var reservation = await _r.Admin.PostOkAsync<ReservationDto>("/api/Reservation", TestRestaurant.Reservation(
                table.Id, _customer.Id, TestRestaurant.FutureEvening(), menuItemIds: new[] { starter.Id, main.Id }, quantities: new[] { 2, 1 }));

            Assert.Equal(31.25m, reservation.TotalAmount);
        }

        [Fact]
        public async Task Unavailable_dish_cannot_be_pre_ordered()
        {
            var table = await _r.AddTableAsync();
            var soldOut = await _r.AddMenuItemAsync(isAvailable: false);

            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, _r.Admin.PostAsync("/api/Reservation", TestRestaurant.Reservation(
                table.Id, _customer.Id, TestRestaurant.FutureEvening(), menuItemIds: new[] { soldOut.Id }, quantities: new[] { 1 })));
        }

        [Fact]
        public async Task Moving_a_reservation_onto_a_booked_slot_is_rejected()
        {
            var table = await _r.AddTableAsync();
            var when = TestRestaurant.FutureEvening(18);
            await _r.ReserveAsync(table.Id, _customer.Id, when);
            var later = await _r.ReserveAsync(table.Id, _customer.Id, when.AddHours(3));

            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, _r.Admin.PutAsync($"/api/Reservation/{later.Id}",
                TestRestaurant.Reservation(table.Id, _customer.Id, when.AddHours(1))));
        }
    }
}

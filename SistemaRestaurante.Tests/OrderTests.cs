using System.Net;
using SistemaRestaurante.Tests.Infrastructure;

namespace SistemaRestaurante.Tests
{
    /// <summary>Servicio en mesa: cuentas, envío a cocina, pantalla de cocina, cuenta y cierre.</summary>
    public class OrderTests : IClassFixture<ApiFactory>, IAsyncLifetime
    {
        private readonly ApiFactory _api;
        private TestRestaurant _restaurant = null!;
        private ApiClient _waiter = null!;
        private ApiClient _manager = null!;
        private ApiClient _kitchen = null!;
        private StationDto _cocina = null!;
        private StationDto _barra = null!;
        private MenuItemDto _main = null!;
        private MenuItemDto _starter = null!;
        private MenuItemDto _drink = null!;

        public OrderTests(ApiFactory api)
        {
            _api = api;
        }

        public async Task InitializeAsync()
        {
            _restaurant = await _api.CreateRestaurantAsync();
            _waiter = await _restaurant.AddStaffAsync("Staff");
            _manager = await _restaurant.AddStaffAsync("Manager");
            _kitchen = await _restaurant.AddStaffAsync("Kitchen");
            var stations = await _restaurant.Admin.GetOkAsync<List<StationDto>>("/api/Station");
            _cocina = stations.Single(s => s.Name == "Cocina");
            _barra = stations.Single(s => s.Name == "Barra");
            _main = await _restaurant.AddMenuItemAsync(25000m);
            _starter = await _restaurant.AddMenuItemAsync(12000m, category: "Appetizer");
            _drink = await _restaurant.AddMenuItemAsync(6000m, category: "Drink");
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // ---------- Ayudas ----------

        private async Task<OrderDto> OpenAsync(int? tableId = null, int guests = 2, ApiClient? as_ = null)
        {
            tableId ??= (await _restaurant.AddTableAsync()).Id;
            return await (as_ ?? _waiter).PostOkAsync<OrderDto>("/api/Order", new { tableId, guests });
        }

        private Task<OrderDto> AddAsync(int orderId, params object[] items) =>
            _waiter.PostOkAsync<OrderDto>($"/api/Order/{orderId}/items", new { items });

        private static object Item(MenuItemDto dish, int quantity = 1, string? notes = null, string? course = null, Guid? requestId = null) =>
            new { menuItemId = dish.Id, quantity, notes, course, clientRequestId = requestId };

        private Task<OrderDto> SendAsync(int orderId, string? holdFromCourse = null) =>
            _waiter.PostOkAsync<OrderDto>($"/api/Order/{orderId}/send", new { holdFromCourse });

        private Task<List<TicketDto>> TicketsAsync(int? stationId = null, ApiClient? as_ = null) =>
            (as_ ?? _kitchen).GetOkAsync<List<TicketDto>>("/api/Kitchen/tickets" + (stationId is int id ? $"?stationId={id}" : ""));

        private Task<OrderDto> GetAsync(int orderId) => _waiter.GetOkAsync<OrderDto>($"/api/Order/{orderId}");

        private async Task ServeAllAsync(OrderDto order)
        {
            foreach (var item in order.Items.Where(i => i.Status is "Sent" or "Preparing" or "Ready"))
                await HttpAssert.StatusAsync(HttpStatusCode.OK,
                    _waiter.PutAsync($"/api/Order/{order.Id}/items/{item.Id}/status", new { status = "Served" }));
        }

        // ---------- Zonas ----------

        [Fact]
        public async Task New_restaurant_gets_kitchen_and_bar_and_dishes_go_to_the_right_one()
        {
            Assert.Equal("Kitchen", _cocina.Type);
            Assert.Equal("Bar", _barra.Type);
            Assert.Equal(_cocina.Id, _main.StationId);
            Assert.Equal("Cocina", _main.StationName);
            Assert.Equal(_barra.Id, _drink.StationId);

            // Un plato puede ir a otra zona si se indica.
            var grill = await _restaurant.Admin.PostOkAsync<StationDto>("/api/Station",
                new { name = "Parrilla", type = "Kitchen", sortOrder = 3, printerAddress = "192.168.1.50:9100" });
            var steak = await _restaurant.AddMenuItemAsync(40000m, stationId: grill.Id);
            Assert.Equal(grill.Id, steak.StationId);
            Assert.Equal("192.168.1.50:9100", grill.PrinterAddress);
        }

        [Fact]
        public async Task Station_with_dishes_or_the_last_one_cannot_be_deleted()
        {
            var other = await _api.CreateRestaurantAsync();
            var stations = await other.Admin.GetOkAsync<List<StationDto>>("/api/Station");
            var cocina = stations.Single(s => s.Name == "Cocina");
            var barra = stations.Single(s => s.Name == "Barra");
            await other.AddMenuItemAsync(10000m); // a Cocina

            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, other.Admin.DeleteAsync($"/api/Station/{cocina.Id}"));
            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, other.Admin.DeleteAsync($"/api/Station/{barra.Id}"));

            var extra = await other.Admin.PostOkAsync<StationDto>("/api/Station", new { name = "Postres", type = "Kitchen", sortOrder = 5 });
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict,
                other.Admin.PostAsync("/api/Station", new { name = "postres", type = "Other" }));
            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, other.Admin.DeleteAsync($"/api/Station/{extra.Id}"));
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, other.Admin.DeleteAsync($"/api/Station/{cocina.Id}"));
        }

        [Fact]
        public async Task Only_management_manages_stations_but_everyone_sees_them()
        {
            await HttpAssert.StatusAsync(HttpStatusCode.OK, _kitchen.GetAsync("/api/Station"));
            await HttpAssert.StatusAsync(HttpStatusCode.OK, _waiter.GetAsync("/api/Station"));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _waiter.PostAsync("/api/Station", new { name = "X", type = "Bar" }));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _kitchen.DeleteAsync($"/api/Station/{_barra.Id}"));
        }

        // ---------- Flujo completo ----------

        [Fact]
        public async Task Full_service_from_seating_to_payment()
        {
            var table = await _restaurant.AddTableAsync();
            var order = await OpenAsync(table.Id, guests: 3);
            Assert.Equal("Open", order.Status);
            Assert.Equal(table.TableNumber, order.TableNumber);
            Assert.Equal("Occupied", (await _waiter.GetOkAsync<TableDto>($"/api/Table/{table.Id}")).Status);

            order = await AddAsync(order.Id, Item(_main, 2, "sin cebolla"), Item(_drink, 2));
            Assert.All(order.Items, i => Assert.Equal("Draft", i.Status));
            Assert.Equal(0m, order.Subtotal); // lo apuntado aún no cuenta

            // Cocina no ve lo que no se ha enviado.
            Assert.DoesNotContain(await TicketsAsync(), t => t.OrderId == order.Id);

            order = await SendAsync(order.Id);
            Assert.All(order.Items, i => Assert.Equal("Sent", i.Status));
            Assert.Equal(62000m, order.Subtotal);

            // Cada zona ve solo lo suyo.
            var kitchenTicket = Assert.Single(await TicketsAsync(_cocina.Id), t => t.OrderId == order.Id);
            var mainLine = Assert.Single(kitchenTicket.Items);
            Assert.Equal(2, mainLine.Quantity);
            Assert.Equal("sin cebolla", mainLine.Notes);
            var barTicket = Assert.Single(await TicketsAsync(_barra.Id), t => t.OrderId == order.Id);
            Assert.Equal(table.TableNumber, barTicket.TableNumber);

            // Cocina: en preparación y despacho de la comanda.
            await HttpAssert.StatusAsync(HttpStatusCode.OK, _kitchen.PutAsync(
                $"/api/Kitchen/orders/{order.Id}/items/{mainLine.Id}/status", new { status = "Preparing", expectedStatus = "Sent" }));
            await HttpAssert.StatusAsync(HttpStatusCode.NoContent,
                _kitchen.PostAsync($"/api/Kitchen/orders/{order.Id}/stations/{_cocina.Id}/bump"));
            await HttpAssert.StatusAsync(HttpStatusCode.NoContent,
                _waiter.PostAsync($"/api/Kitchen/orders/{order.Id}/stations/{_barra.Id}/bump"));

            order = await GetAsync(order.Id);
            Assert.All(order.Items, i => Assert.Equal("Ready", i.Status));

            // Cuenta provisional: 2 x 25.000 + 2 x 6.000 = 62.000; propina sugerida 10 % = 6.200.
            var bill = await _waiter.GetOkAsync<BillDto>($"/api/Order/{order.Id}/bill");
            Assert.Equal("COP", bill.CurrencyCode);
            Assert.Equal(62000m, bill.Subtotal);
            Assert.Equal(10m, bill.SuggestedTipPercent);
            Assert.Equal(6200m, bill.SuggestedTip);
            Assert.Equal(68200m, bill.TotalWithSuggestedTip);
            Assert.Equal(2, bill.PendingItems); // listos pero sin servir

            await ServeAllAsync(order);
            var closed = await _waiter.PostOkAsync<OrderDto>($"/api/Order/{order.Id}/close",
                new { paymentMethod = "Card", tipAmount = 5000m });
            Assert.Equal("Closed", closed.Status);
            Assert.Equal(62000m, closed.Subtotal);
            Assert.Equal(5000m, closed.TipAmount);
            Assert.Equal("Card", closed.PaymentMethod);
            Assert.Equal("Available", (await _waiter.GetOkAsync<TableDto>($"/api/Table/{table.Id}")).Status);

            // Ya no está entre las abiertas ni en cocina, pero sí en el histórico del día.
            Assert.DoesNotContain(await _waiter.GetOkAsync<List<OrderDto>>("/api/Order/open"), o => o.Id == order.Id);
            Assert.DoesNotContain(await TicketsAsync(), t => t.OrderId == order.Id);
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            Assert.Contains(await _manager.GetOkAsync<List<OrderDto>>($"/api/Order/history?date={today}"), o => o.Id == order.Id);

            // Una cuenta cerrada no admite más cambios.
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict,
                _waiter.PostAsync($"/api/Order/{order.Id}/items", new { items = new[] { Item(_drink) } }));
        }

        [Fact]
        public async Task Suggested_tip_is_rounded_to_whole_pesos()
        {
            var dish = await _restaurant.AddMenuItemAsync(12345m);
            var order = await OpenAsync();
            await AddAsync(order.Id, Item(dish));
            await SendAsync(order.Id);

            var bill = await _waiter.GetOkAsync<BillDto>($"/api/Order/{order.Id}/bill");
            Assert.Equal(1235m, bill.SuggestedTip); // 1.234,5 → 1.235
        }

        // ---------- Reglas ----------

        [Fact]
        public async Task A_table_has_only_one_open_order_even_when_opened_at_the_same_time()
        {
            var table = await _restaurant.AddTableAsync();
            var attempts = Enumerable.Range(0, 5)
                .Select(_ => _waiter.StatusOfAsync(_waiter.PostAsync("/api/Order", new { tableId = table.Id, guests = 2 })));
            var results = await Task.WhenAll(attempts);

            Assert.Single(results, s => s == HttpStatusCode.Created);
            Assert.All(results.Where(s => s != HttpStatusCode.Created), s => Assert.Equal(HttpStatusCode.Conflict, s));
        }

        [Fact]
        public async Task Out_of_service_table_and_bad_guests_are_rejected()
        {
            var broken = await _restaurant.AddTableAsync(status: "OutOfService");
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, _waiter.PostAsync("/api/Order", new { tableId = broken.Id, guests = 2 }));

            var table = await _restaurant.AddTableAsync();
            await HttpAssert.StatusAsync(HttpStatusCode.BadRequest, _waiter.PostAsync("/api/Order", new { tableId = table.Id, guests = 0 }));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, _waiter.PostAsync("/api/Order", new { tableId = 999999, guests = 2 }));
        }

        [Fact]
        public async Task Retried_request_does_not_duplicate_dishes()
        {
            var order = await OpenAsync();
            var requestId = Guid.NewGuid();

            await AddAsync(order.Id, Item(_main, requestId: requestId));
            order = await AddAsync(order.Id, Item(_main, requestId: requestId), Item(_drink, requestId: Guid.NewGuid()));

            Assert.Equal(2, order.Items.Count);
            Assert.Single(order.Items, i => i.ClientRequestId == requestId);
        }

        [Fact]
        public async Task Unavailable_dish_and_bad_quantity_are_rejected()
        {
            var soldOut = await _restaurant.AddMenuItemAsync(9000m, isAvailable: false);
            var order = await OpenAsync();

            await HttpAssert.StatusAsync(HttpStatusCode.Conflict,
                _waiter.PostAsync($"/api/Order/{order.Id}/items", new { items = new[] { Item(soldOut) } }));
            await HttpAssert.StatusAsync(HttpStatusCode.BadRequest,
                _waiter.PostAsync($"/api/Order/{order.Id}/items", new { items = new[] { Item(_main, quantity: 0) } }));
            Assert.Empty((await GetAsync(order.Id)).Items);
        }

        [Fact]
        public async Task Price_is_kept_when_the_menu_changes()
        {
            var dish = await _restaurant.AddMenuItemAsync(20000m);
            var order = await OpenAsync();
            await AddAsync(order.Id, Item(dish));

            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, _restaurant.Admin.PutAsync($"/api/MenuItem/{dish.Id}",
                new { name = "Nuevo nombre", price = 30000m, category = "MainCourse", isAvailable = true }));

            var line = Assert.Single((await GetAsync(order.Id)).Items);
            Assert.Equal(20000m, line.UnitPrice);
            Assert.Equal(dish.Name, line.Name);
        }

        [Fact]
        public async Task Drafts_can_be_edited_but_sent_dishes_need_a_manager_to_cancel()
        {
            var order = await OpenAsync();
            order = await AddAsync(order.Id, Item(_main), Item(_drink));
            var main = order.Items.Single(i => i.MenuItemId == _main.Id);
            var drink = order.Items.Single(i => i.MenuItemId == _drink.Id);

            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, _waiter.PutAsync($"/api/Order/{order.Id}/items/{main.Id}",
                new { quantity = 3, notes = "poco hecho", course = "Main" }));
            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, _waiter.DeleteAsync($"/api/Order/{order.Id}/items/{drink.Id}"));

            order = await SendAsync(order.Id);
            main = Assert.Single(order.Items);
            Assert.Equal(3, main.Quantity);
            Assert.Equal("poco hecho", main.Notes);

            // Enviado: el camarero ya no puede tocarlo ni quitarlo.
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, _waiter.DeleteAsync($"/api/Order/{order.Id}/items/{main.Id}"));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden,
                _waiter.PostAsync($"/api/Order/{order.Id}/items/{main.Id}/cancel", new { reason = "Se equivocó" }));
            await HttpAssert.StatusAsync(HttpStatusCode.BadRequest,
                _manager.PostAsync($"/api/Order/{order.Id}/items/{main.Id}/cancel", new { reason = "" }));
            await HttpAssert.StatusAsync(HttpStatusCode.NoContent,
                _manager.PostAsync($"/api/Order/{order.Id}/items/{main.Id}/cancel", new { reason = "El cliente cambió de idea" }));

            order = await GetAsync(order.Id);
            Assert.Equal("Cancelled", Assert.Single(order.Items).Status);
            Assert.Equal(0m, order.Subtotal);
            Assert.DoesNotContain(await TicketsAsync(), t => t.OrderId == order.Id);
        }

        [Fact]
        public async Task Held_courses_wait_until_they_are_fired()
        {
            var order = await OpenAsync();
            await AddAsync(order.Id, Item(_drink), Item(_starter), Item(_main));
            order = await SendAsync(order.Id, holdFromCourse: "Main");

            Assert.Equal("Sent", order.Items.Single(i => i.MenuItemId == _drink.Id).Status);
            Assert.Equal("Sent", order.Items.Single(i => i.MenuItemId == _starter.Id).Status);
            Assert.Equal("Held", order.Items.Single(i => i.MenuItemId == _main.Id).Status);

            // Cocina ve el segundo como retenido (para saber lo que viene), sin poder despacharlo.
            var ticket = Assert.Single(await TicketsAsync(_cocina.Id), t => t.OrderId == order.Id);
            Assert.Contains(ticket.Items, i => i.Status == "Held");

            order = await _waiter.PostOkAsync<OrderDto>($"/api/Order/{order.Id}/fire", new { course = "Main" });
            Assert.Equal("Sent", order.Items.Single(i => i.MenuItemId == _main.Id).Status);
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, _waiter.PostAsync($"/api/Order/{order.Id}/fire", new { course = "Main" }));
        }

        [Fact]
        public async Task Cannot_close_while_the_kitchen_is_still_working_and_unsent_drafts_are_dropped()
        {
            var order = await OpenAsync();
            await AddAsync(order.Id, Item(_main));
            order = await SendAsync(order.Id);
            await AddAsync(order.Id, Item(_drink)); // apuntado y olvidado

            await HttpAssert.StatusAsync(HttpStatusCode.Conflict,
                _waiter.PostAsync($"/api/Order/{order.Id}/close", new { paymentMethod = "Cash", tipAmount = 0 }));

            await HttpAssert.StatusAsync(HttpStatusCode.NoContent,
                _kitchen.PostAsync($"/api/Kitchen/orders/{order.Id}/stations/{_cocina.Id}/bump"));
            // Listo en el pase: al cobrar se da por servido.
            var closed = await _waiter.PostOkAsync<OrderDto>($"/api/Order/{order.Id}/close", new { paymentMethod = "Cash", tipAmount = 0 });

            var line = Assert.Single(closed.Items);
            Assert.Equal("Served", line.Status);
            Assert.Equal(25000m, closed.Subtotal);
        }

        [Fact]
        public async Task Kitchen_status_changes_detect_other_devices_and_invalid_moves()
        {
            var order = await OpenAsync();
            await AddAsync(order.Id, Item(_main));
            order = await SendAsync(order.Id);
            var item = Assert.Single(order.Items);
            var url = $"/api/Kitchen/orders/{order.Id}/items/{item.Id}/status";

            await HttpAssert.StatusAsync(HttpStatusCode.OK, _kitchen.PutAsync(url, new { status = "Ready", expectedStatus = "Sent" }));
            // Otra pantalla aún lo veía como enviado.
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, _kitchen.PutAsync(url, new { status = "Preparing", expectedStatus = "Sent" }));
            // Servir en la mesa no es cosa de cocina.
            await HttpAssert.StatusAsync(HttpStatusCode.BadRequest, _kitchen.PutAsync(url, new { status = "Served" }));
            // Deshacer: vuelve a preparación.
            await HttpAssert.StatusAsync(HttpStatusCode.OK, _kitchen.PutAsync(url, new { status = "Preparing", expectedStatus = "Ready" }));
            // Un borrador no se puede marcar desde cocina.
            order = await AddAsync(order.Id, Item(_starter));
            var draft = order.Items.Single(i => i.Status == "Draft");
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict,
                _kitchen.PutAsync($"/api/Kitchen/orders/{order.Id}/items/{draft.Id}/status", new { status = "Ready" }));
        }

        [Fact]
        public async Task Kitchen_role_only_uses_the_kitchen_screen()
        {
            var table = await _restaurant.AddTableAsync();
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _kitchen.PostAsync("/api/Order", new { tableId = table.Id, guests = 2 }));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _kitchen.GetAsync("/api/Order/open"));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _kitchen.GetAsync("/api/Reservation"));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _kitchen.GetAsync("/api/Customer"));
            await HttpAssert.StatusAsync(HttpStatusCode.OK, _kitchen.GetAsync("/api/Kitchen/tickets"));
            await HttpAssert.StatusAsync(HttpStatusCode.OK, _kitchen.GetAsync("/api/Restaurant/current"));

            // El histórico y anular cuentas es del encargado.
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _waiter.GetAsync("/api/Order/history?date=2026-01-01"));
            var order = await OpenAsync(table.Id);
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _waiter.PostAsync($"/api/Order/{order.Id}/cancel", new { reason = "Se fueron" }));
        }

        [Fact]
        public async Task Manager_cancels_a_whole_order_and_the_table_is_freed()
        {
            var table = await _restaurant.AddTableAsync();
            var order = await OpenAsync(table.Id);
            await AddAsync(order.Id, Item(_main));
            await SendAsync(order.Id);

            await HttpAssert.StatusAsync(HttpStatusCode.NoContent,
                _manager.PostAsync($"/api/Order/{order.Id}/cancel", new { reason = "Los clientes se fueron" }));

            order = await GetAsync(order.Id);
            Assert.Equal("Cancelled", order.Status);
            Assert.All(order.Items, i => Assert.Equal("Cancelled", i.Status));
            Assert.Equal("Available", (await _waiter.GetOkAsync<TableDto>($"/api/Table/{table.Id}")).Status);
            Assert.DoesNotContain(await TicketsAsync(), t => t.OrderId == order.Id);
        }

        [Fact]
        public async Task Order_can_move_to_a_free_table_only()
        {
            var first = await _restaurant.AddTableAsync();
            var second = await _restaurant.AddTableAsync();
            var busy = await _restaurant.AddTableAsync();
            var order = await OpenAsync(first.Id);
            await OpenAsync(busy.Id);

            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, _waiter.PutAsync($"/api/Order/{order.Id}/table", new { tableId = busy.Id }));
            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, _waiter.PutAsync($"/api/Order/{order.Id}/table", new { tableId = second.Id }));
            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, _waiter.PutAsync($"/api/Order/{order.Id}/guests", new { guests = 6 }));

            order = await GetAsync(order.Id);
            Assert.Equal(second.Id, order.TableId);
            Assert.Equal(6, order.Guests);
            Assert.Equal("Available", (await _waiter.GetOkAsync<TableDto>($"/api/Table/{first.Id}")).Status);
            Assert.Equal("Occupied", (await _waiter.GetOkAsync<TableDto>($"/api/Table/{second.Id}")).Status);
        }

        [Fact]
        public async Task Seating_a_reservation_completes_it_and_brings_its_preorder()
        {
            var table = await _restaurant.AddTableAsync();
            var customer = await _restaurant.AddCustomerAsync();
            var reservation = await _restaurant.Admin.PostOkAsync<ReservationDto>("/api/Reservation",
                TestRestaurant.Reservation(table.Id, customer.Id, TestRestaurant.FutureEvening(), 2,
                    new[] { _main.Id, _drink.Id }, new[] { 2, 1 }));

            var order = await _waiter.PostOkAsync<OrderDto>("/api/Order",
                new { tableId = table.Id, guests = 2, reservationId = reservation.Id });

            Assert.Equal(reservation.Id, order.ReservationId);
            Assert.Equal(2, order.Items.Count);
            Assert.All(order.Items, i => Assert.Equal("Draft", i.Status)); // el camarero lo confirma antes de enviar
            Assert.Equal(2, order.Items.Single(i => i.MenuItemId == _main.Id).Quantity);
            Assert.Equal("Completed", (await _waiter.GetOkAsync<ReservationDto>($"/api/Reservation/{reservation.Id}")).Status);

            // Una reserva ya usada no sirve para otra mesa.
            var other = await _restaurant.AddTableAsync();
            await HttpAssert.StatusAsync(HttpStatusCode.Conflict,
                _waiter.PostAsync("/api/Order", new { tableId = other.Id, guests = 2, reservationId = reservation.Id }));
        }

        // ---------- Aislamiento ----------

        [Fact]
        public async Task Another_restaurant_cannot_see_or_touch_orders()
        {
            var order = await OpenAsync();
            await AddAsync(order.Id, Item(_main));
            await SendAsync(order.Id);

            var other = await _api.CreateRestaurantAsync();
            var otherKitchen = await other.AddStaffAsync("Kitchen");
            var otherTable = await other.AddTableAsync();

            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, other.Admin.GetAsync($"/api/Order/{order.Id}"));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound,
                other.Admin.PostAsync($"/api/Order/{order.Id}/items", new { items = new[] { Item(_main) } }));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound,
                otherKitchen.PostAsync($"/api/Kitchen/orders/{order.Id}/stations/{_cocina.Id}/bump"));
            Assert.Empty(await TicketsAsync(as_: otherKitchen));
            Assert.Empty(await other.Admin.GetOkAsync<List<OrderDto>>("/api/Order/open"));

            // Ni con sus propios IDs puede usar mesas, platos o zonas de otro restaurante.
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound,
                other.Admin.PostAsync("/api/Order", new { tableId = order.TableId, guests = 2 }));
            var otherOrder = await other.Admin.PostOkAsync<OrderDto>("/api/Order", new { tableId = otherTable.Id, guests = 2 });
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound,
                other.Admin.PostAsync($"/api/Order/{otherOrder.Id}/items", new { items = new[] { Item(_main) } }));
            await HttpAssert.StatusAsync(HttpStatusCode.NotFound, other.Admin.PostAsync("/api/MenuItem",
                new { name = "Intruso", price = 1000m, category = "MainCourse", isAvailable = true, stationId = _cocina.Id }));
        }
    }
}

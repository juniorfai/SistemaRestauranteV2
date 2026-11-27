using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using SistemaRestaurante.Tests.Infrastructure;

namespace SistemaRestaurante.Tests
{
    /// <summary>
    /// Tiempo real (SignalR en /api/hubs/orders): cada pantalla recibe solo los avisos de su
    /// restaurante, y el camarero se entera de los platos listos.
    /// </summary>
    public class RealtimeTests : IClassFixture<ApiFactory>, IAsyncLifetime
    {
        private const string HubPath = "/api/hubs/orders";
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

        private readonly ApiFactory _api;
        private readonly List<HubConnection> _connections = new();
        private TestRestaurant _restaurant = null!;
        private ApiClient _waiter = null!;
        private ApiClient _kitchen = null!;
        private MenuItemDto _main = null!;
        private MenuItemDto _drink = null!;

        public RealtimeTests(ApiFactory api)
        {
            _api = api;
        }

        public async Task InitializeAsync()
        {
            _restaurant = await _api.CreateRestaurantAsync();
            _waiter = await _restaurant.AddStaffAsync("Staff");
            _kitchen = await _restaurant.AddStaffAsync("Kitchen");
            _main = await _restaurant.AddMenuItemAsync(25000m);
            _drink = await _restaurant.AddMenuItemAsync(6000m, category: "Drink");
        }

        public async Task DisposeAsync()
        {
            foreach (var connection in _connections)
                await connection.DisposeAsync();
        }

        // ---------- Ayudas ----------

        public record OrderChangedMessage(int OrderId);
        public record ItemsReadyMessage(int OrderId, int TableNumber, int WaiterId, List<string> Items);

        /// <summary>Pantalla conectada que guarda los avisos que le llegan.</summary>
        private sealed class Screen
        {
            public ConcurrentQueue<OrderChangedMessage> Changed { get; } = new();
            public ConcurrentQueue<ItemsReadyMessage> Ready { get; } = new();

            public async Task<T> WaitForAsync<T>(ConcurrentQueue<T> queue, Func<T, bool> match)
            {
                var until = DateTime.UtcNow + Wait;
                while (DateTime.UtcNow < until)
                {
                    var found = queue.FirstOrDefault(match);
                    if (found is not null) return found;
                    await Task.Delay(50);
                }
                throw new TimeoutException($"No llegó el aviso esperado ({typeof(T).Name}).");
            }
        }

        /// <summary>Conexión por long polling a través del servidor de pruebas, con el token en la cabecera.</summary>
        private async Task<Screen> ConnectAsync(ApiClient client)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(new Uri(_api.Server.BaseAddress, HubPath), options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => _api.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult<string?>(client.Session!.Token);
                })
                .Build();
            var screen = Listen(connection);
            await connection.StartAsync();
            return screen;
        }

        private Screen Listen(HubConnection connection)
        {
            _connections.Add(connection);
            var screen = new Screen();
            connection.On<OrderChangedMessage>("OrderChanged", m => screen.Changed.Enqueue(m));
            connection.On<ItemsReadyMessage>("ItemsReady", m => screen.Ready.Enqueue(m));
            return screen;
        }

        private async Task<OrderDto> SentOrderAsync()
        {
            var table = await _restaurant.AddTableAsync();
            var order = await _waiter.PostOkAsync<OrderDto>("/api/Order", new { tableId = table.Id, guests = 2 });
            await _waiter.PostOkAsync<OrderDto>($"/api/Order/{order.Id}/items", new
            {
                items = new object[]
                {
                    new { menuItemId = _main.Id, quantity = 2 },
                    new { menuItemId = _drink.Id, quantity = 1 },
                },
            });
            return await _waiter.PostOkAsync<OrderDto>($"/api/Order/{order.Id}/send", new { holdFromCourse = (string?)null });
        }

        // ---------- Pruebas ----------

        [Fact]
        public async Task Changes_reach_only_the_screens_of_the_same_restaurant()
        {
            var kitchenScreen = await ConnectAsync(_kitchen);
            var other = await _api.CreateRestaurantAsync();
            var otherScreen = await ConnectAsync(other.Admin);

            var order = await SentOrderAsync();

            await kitchenScreen.WaitForAsync(kitchenScreen.Changed, m => m.OrderId == order.Id);
            await Task.Delay(500);
            Assert.Empty(otherScreen.Changed);
        }

        [Fact]
        public async Task Waiter_is_told_which_dishes_are_ready()
        {
            var waiterScreen = await ConnectAsync(_waiter);
            var order = await SentOrderAsync();
            var main = order.Items.Single(i => i.MenuItemId == _main.Id);
            var drink = order.Items.Single(i => i.MenuItemId == _drink.Id);

            // Pasar un plato a "en preparación" no avisa al camarero; dejarlo listo, sí.
            await HttpAssert.StatusAsync(HttpStatusCode.OK, _kitchen.PutAsync(
                $"/api/Kitchen/orders/{order.Id}/items/{drink.Id}/status", new { status = "Preparing" }));
            await HttpAssert.StatusAsync(HttpStatusCode.OK, _kitchen.PutAsync(
                $"/api/Kitchen/orders/{order.Id}/items/{drink.Id}/status", new { status = "Ready" }));
            var single = await waiterScreen.WaitForAsync(waiterScreen.Ready, m => m.OrderId == order.Id);
            Assert.Equal(new[] { drink.Name }, single.Items);
            Assert.Equal(order.TableNumber, single.TableNumber);
            Assert.Equal(_waiter.Session!.User.Id, single.WaiterId);

            // Despachar el ticket de cocina avisa de todo lo que salía de esa zona.
            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, _kitchen.PostAsync(
                $"/api/Kitchen/orders/{order.Id}/stations/{main.StationId}/bump"));
            var bumped = await waiterScreen.WaitForAsync(waiterScreen.Ready, m => m.Items.Contains($"2 x {main.Name}"));
            Assert.Equal(order.Id, bumped.OrderId);
            Assert.Single(bumped.Items);
        }

        [Fact]
        public async Task Browser_style_websocket_with_the_token_in_the_url_works()
        {
            // El navegador no puede mandar cabeceras en un WebSocket: el token va en ?access_token=.
            var token = Uri.EscapeDataString(_kitchen.Session!.Token);
            var connection = new HubConnectionBuilder()
                .WithUrl(new Uri(_api.Server.BaseAddress, HubPath), options =>
                {
                    options.Transports = HttpTransportType.WebSockets;
                    options.SkipNegotiation = true;
                    options.WebSocketFactory = async (context, cancellationToken) =>
                    {
                        var url = new UriBuilder(context.Uri) { Query = $"access_token={token}" }.Uri;
                        return await _api.Server.CreateWebSocketClient().ConnectAsync(url, cancellationToken);
                    };
                })
                .Build();
            var screen = Listen(connection);
            await connection.StartAsync();

            var order = await SentOrderAsync();
            await screen.WaitForAsync(screen.Changed, m => m.OrderId == order.Id);
        }

        [Fact]
        public async Task Hub_requires_a_restaurant_session()
        {
            var negotiate = $"{HubPath}/negotiate?negotiateVersion=1";
            await HttpAssert.StatusAsync(HttpStatusCode.Unauthorized, _api.Anonymous().PostAsync(negotiate));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, (await _api.PlatformAsync()).PostAsync(negotiate));
            await HttpAssert.StatusAsync(HttpStatusCode.OK, _kitchen.PostAsync(negotiate));

            // El token en la URL solo vale para el hub, no para el resto de la API.
            var token = Uri.EscapeDataString(_kitchen.Session!.Token);
            var anonymous = _api.Anonymous();
            await HttpAssert.StatusAsync(HttpStatusCode.OK, anonymous.PostAsync($"{negotiate}&access_token={token}"));
            await HttpAssert.StatusAsync(HttpStatusCode.Unauthorized, anonymous.GetAsync($"/api/Order/open?access_token={token}"));
        }
    }
}

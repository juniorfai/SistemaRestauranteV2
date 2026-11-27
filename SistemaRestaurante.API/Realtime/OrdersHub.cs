using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SistemaRestaurante.API.Auth;
using SistemaRestaurante.Domain.Constants;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.API.Realtime
{
    /// <summary>
    /// Canal en tiempo real de las comandas (/api/hubs/orders). Cada conexión entra solo en el
    /// grupo de su restaurante, así nadie recibe avisos de otro. Los clientes solo escuchan:
    /// todos los cambios se hacen por la API REST.
    /// </summary>
    [Authorize(Roles = Roles.AllRestaurantRoles)]
    public class OrdersHub : Hub
    {
        public const string Path = "/api/hubs/orders";

        /// <summary>Algo cambió en una cuenta: { orderId }.</summary>
        public const string OrderChanged = "OrderChanged";

        /// <summary>Cocina dejó platos listos para servir: { orderId, tableNumber, waiterId, items }.</summary>
        public const string ItemsReady = "ItemsReady";

        public static string GroupFor(int restaurantId) => $"restaurant-{restaurantId}";

        public override async Task OnConnectedAsync()
        {
            if (!int.TryParse(Context.User?.FindFirstValue(TokenService.RestaurantClaim), out var restaurantId))
            {
                Context.Abort();
                return;
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(restaurantId));
            await base.OnConnectedAsync();
        }
    }

    /// <summary>Envía los avisos de las comandas a las pantallas del restaurante.</summary>
    public class SignalROrderNotifier : IOrderNotifier
    {
        private readonly IHubContext<OrdersHub> _hub;
        private readonly ILogger<SignalROrderNotifier> _logger;

        public SignalROrderNotifier(IHubContext<OrdersHub> hub, ILogger<SignalROrderNotifier> logger)
        {
            _hub = hub;
            _logger = logger;
        }

        public Task OrderChangedAsync(int restaurantId, int orderId) =>
            SendAsync(restaurantId, OrdersHub.OrderChanged, new { orderId });

        public Task ItemsReadyAsync(int restaurantId, int orderId, int tableNumber, int waiterId, IReadOnlyList<string> items) =>
            SendAsync(restaurantId, OrdersHub.ItemsReady, new { orderId, tableNumber, waiterId, items });

        private async Task SendAsync(int restaurantId, string method, object payload)
        {
            // El aviso es un extra: si falla, la operación ya está guardada y las pantallas
            // se ponen al día en su siguiente recarga.
            try
            {
                await _hub.Clients.Group(OrdersHub.GroupFor(restaurantId)).SendAsync(method, payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not send {Method} to restaurant {RestaurantId}", method, restaurantId);
            }
        }
    }
}

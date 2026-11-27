using Microsoft.Extensions.Logging;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.Domain.Services
{
    /// <summary>
    /// Servicio en mesa: el camarero abre la cuenta, apunta platos y los envía a cocina; cocina
    /// los prepara y marca listos; el camarero los sirve y, al final, cierra la cuenta.
    /// Todos los repositorios comparten el mismo contexto, así que cada operación se guarda de una vez.
    /// </summary>
    public class OrderService : IOrderService
    {
        public const int MaxGuests = 50;
        public const int MaxQuantity = 99;
        public const int MaxNotesLength = 200;

        private static readonly OrderItemStatus[] InKitchen =
            { OrderItemStatus.Held, OrderItemStatus.Sent, OrderItemStatus.Preparing };

        /// <summary>Cambios de estado permitidos para un plato ya enviado (incluye deshacer).</summary>
        private static readonly Dictionary<OrderItemStatus, OrderItemStatus[]> Transitions = new()
        {
            [OrderItemStatus.Sent] = new[] { OrderItemStatus.Preparing, OrderItemStatus.Ready, OrderItemStatus.Served },
            [OrderItemStatus.Preparing] = new[] { OrderItemStatus.Sent, OrderItemStatus.Ready, OrderItemStatus.Served },
            [OrderItemStatus.Ready] = new[] { OrderItemStatus.Preparing, OrderItemStatus.Served },
            [OrderItemStatus.Served] = new[] { OrderItemStatus.Ready },
        };

        private readonly IOrderRepository _orders;
        private readonly ITableRepository _tables;
        private readonly IReservationRepository _reservations;
        private readonly IMenuItemRepository _menuItems;
        private readonly IRestaurantRepository _restaurants;
        private readonly IOrderNotifier _notifier;
        private readonly ITenantContext _tenant;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orders,
            ITableRepository tables,
            IReservationRepository reservations,
            IMenuItemRepository menuItems,
            IRestaurantRepository restaurants,
            IOrderNotifier notifier,
            ITenantContext tenant,
            ILogger<OrderService> logger)
        {
            _orders = orders;
            _tables = tables;
            _reservations = reservations;
            _menuItems = menuItems;
            _restaurants = restaurants;
            _notifier = notifier;
            _tenant = tenant;
            _logger = logger;
        }

        // ---------- Consultas ----------

        public Task<IEnumerable<Order>> GetOpenAsync() => _orders.GetOpenAsync();

        public async Task<Order> GetAsync(int id) =>
            await _orders.GetDetailAsync(id) ?? throw new KeyNotFoundException($"No se encontró la cuenta con ID {id}");

        public Task<IEnumerable<Order>> GetClosedOnAsync(DateOnly localDate)
        {
            // El día se cuenta en la hora local del restaurante (TZ del servidor, p. ej. America/Bogota).
            var from = TimeZoneInfo.ConvertTimeToUtc(localDate.ToDateTime(TimeOnly.MinValue), TimeZoneInfo.Local);
            var to = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeZoneInfo.Local);
            return _orders.GetClosedBetweenAsync(from, to);
        }

        public Task<IEnumerable<OrderItem>> GetKitchenItemsAsync(int? stationId) => _orders.GetKitchenItemsAsync(stationId);

        // ---------- Abrir la cuenta ----------

        public async Task<Order> OpenAsync(int tableId, int guests, int? reservationId, string? notes, int waiterId)
        {
            ValidateGuests(guests);
            var table = await GetUsableTableAsync(tableId);
            if (await _orders.HasOpenOrderForTableAsync(tableId))
                throw new InvalidOperationException($"La mesa {table.TableNumber} ya tiene una cuenta abierta");

            var order = new Order
            {
                RestaurantId = _tenant.RequireRestaurantId(),
                TableId = table.Id,
                WaiterId = waiterId,
                Guests = guests,
                Notes = CleanNotes(notes, 500),
                Status = OrderStatus.Open,
                OpenedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            if (reservationId is int resId)
            {
                // Llegan los clientes de una reserva: se da por cumplida y lo que pidieron por
                // adelantado se apunta en la cuenta (sin enviar, para que el camarero lo revise).
                var reservation = await _reservations.GetReservationWithDetailsAsync(resId)
                    ?? throw new KeyNotFoundException($"No se encontró la reserva con ID {resId}");
                if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
                    throw new InvalidOperationException("La reserva ya no está pendiente ni confirmada");

                reservation.Status = ReservationStatus.Completed;
                reservation.UpdatedAt = DateTime.UtcNow;
                order.ReservationId = reservation.Id;

                foreach (var pre in reservation.ReservationMenuItems)
                {
                    order.Items.Add(NewItem(order, pre.MenuItem, pre.Quantity, null, null, null, pre.UnitPrice));
                }
            }

            table.Status = TableStatus.Occupied;
            table.UpdatedAt = DateTime.UtcNow;

            await _orders.CreateAsync(order);
            _logger.LogInformation("Opened order {OrderId} at table {TableNumber}", order.Id, table.TableNumber);
            await NotifyAsync(order);
            return await GetAsync(order.Id);
        }

        // ---------- Apuntar platos ----------

        public async Task<Order> AddItemsAsync(int orderId, IReadOnlyList<NewOrderItem> items)
        {
            if (items.Count == 0)
                throw new ArgumentException("Indica al menos un plato");

            var order = await GetOpenOrderAsync(orderId);
            var known = order.Items.Where(i => i.IsActive && i.ClientRequestId != null)
                .Select(i => i.ClientRequestId!.Value).ToHashSet();

            foreach (var request in items)
            {
                // Reintento del mismo envío (p. ej. se cortó el Wi-Fi): ya está apuntado.
                if (request.ClientRequestId is Guid requestId && !known.Add(requestId))
                    continue;

                ValidateQuantity(request.Quantity);
                var menuItem = await _menuItems.GetByIdAsync(request.MenuItemId)
                    ?? throw new KeyNotFoundException($"No se encontró el plato con ID {request.MenuItemId}");
                if (!menuItem.IsAvailable)
                    throw new InvalidOperationException($"\"{menuItem.Name}\" no está disponible ahora");

                order.Items.Add(NewItem(order, menuItem, request.Quantity, CleanNotes(request.Notes, MaxNotesLength),
                    request.Course, request.ClientRequestId, menuItem.Price));
            }

            await SaveOrderAsync(order);
            return await GetAsync(order.Id);
        }

        public async Task UpdateDraftItemAsync(int orderId, int itemId, int quantity, string? notes, OrderCourse course)
        {
            ValidateQuantity(quantity);
            var (order, item) = await GetDraftItemAsync(orderId, itemId);
            item.Quantity = quantity;
            item.Notes = CleanNotes(notes, MaxNotesLength);
            item.Course = course;
            item.UpdatedAt = DateTime.UtcNow;
            await SaveOrderAsync(order);
        }

        public async Task RemoveDraftItemAsync(int orderId, int itemId)
        {
            var (order, item) = await GetDraftItemAsync(orderId, itemId);
            item.IsActive = false;
            item.UpdatedAt = DateTime.UtcNow;
            await SaveOrderAsync(order);
        }

        // ---------- Enviar a cocina ----------

        public async Task<Order> SendAsync(int orderId, OrderCourse? holdFromCourse)
        {
            var order = await GetOpenOrderAsync(orderId);
            var drafts = Live(order).Where(i => i.Status == OrderItemStatus.Draft).ToList();
            if (drafts.Count == 0)
                throw new InvalidOperationException("No hay platos apuntados para enviar");

            var now = DateTime.UtcNow;
            foreach (var item in drafts)
            {
                // Los platos que van "después" (p. ej. los segundos) esperan hasta que el camarero los marche.
                var hold = holdFromCourse is OrderCourse from && item.Course != OrderCourse.Immediate && item.Course >= from;
                item.Status = hold ? OrderItemStatus.Held : OrderItemStatus.Sent;
                item.SentAt = now;
                item.UpdatedAt = now;
            }

            await SaveOrderAsync(order);
            _logger.LogInformation("Order {OrderId}: sent {Count} item(s) to the kitchen", order.Id, drafts.Count);
            return await GetAsync(order.Id);
        }

        public async Task<Order> FireAsync(int orderId, OrderCourse upToCourse)
        {
            var order = await GetOpenOrderAsync(orderId);
            var held = Live(order).Where(i => i.Status == OrderItemStatus.Held && i.Course <= upToCourse).ToList();
            if (held.Count == 0)
                throw new InvalidOperationException("No hay platos retenidos de ese tiempo");

            var now = DateTime.UtcNow;
            foreach (var item in held)
            {
                item.Status = OrderItemStatus.Sent;
                item.SentAt = now; // la cocina los ve como recién pedidos
                item.UpdatedAt = now;
            }

            await SaveOrderAsync(order);
            return await GetAsync(order.Id);
        }

        // ---------- Cocina y sala ----------

        public async Task<OrderItem> ChangeItemStatusAsync(int orderId, int itemId, OrderItemStatus newStatus, OrderItemStatus? expectedStatus)
        {
            var item = await _orders.GetItemAsync(orderId, itemId)
                ?? throw new KeyNotFoundException($"No se encontró el plato {itemId} en la cuenta {orderId}");
            EnsureOpen(item.Order);

            if (expectedStatus is OrderItemStatus expected && item.Status != expected)
                throw new InvalidOperationException("El plato ya cambió de estado desde otro dispositivo");
            if (item.Status == newStatus)
                return item;
            if (!Transitions.TryGetValue(item.Status, out var allowed) || !allowed.Contains(newStatus))
                throw new InvalidOperationException($"Un plato en estado {item.Status} no puede pasar a {newStatus}");

            ApplyStatus(item, newStatus, DateTime.UtcNow);
            await _orders.SaveAsync();
            await NotifyAsync(item.Order);
            if (newStatus == OrderItemStatus.Ready)
                await NotifyReadyAsync(item.Order, new[] { item });
            return item;
        }

        public async Task<Order> BumpAsync(int orderId, int stationId)
        {
            var order = await GetOpenOrderAsync(orderId);
            var pending = Live(order)
                .Where(i => i.StationId == stationId && i.Status is OrderItemStatus.Sent or OrderItemStatus.Preparing)
                .ToList();
            if (pending.Count == 0)
                throw new InvalidOperationException("Esta comanda no tiene platos pendientes en esa zona");

            var now = DateTime.UtcNow;
            foreach (var item in pending)
                ApplyStatus(item, OrderItemStatus.Ready, now);

            // Solo cambian los platos: así cocina no choca con el camarero que sigue apuntando.
            await _orders.SaveAsync();
            await NotifyAsync(order);
            await NotifyReadyAsync(order, pending);
            return await GetAsync(order.Id);
        }

        public async Task CancelItemAsync(int orderId, int itemId, string reason, int userId)
        {
            reason = RequireReason(reason);
            var order = await GetOpenOrderAsync(orderId);
            var item = Live(order).FirstOrDefault(i => i.Id == itemId)
                ?? throw new KeyNotFoundException($"No se encontró el plato {itemId} en la cuenta {orderId}");
            if (item.Status == OrderItemStatus.Cancelled)
                return;

            item.Status = OrderItemStatus.Cancelled;
            item.CancelledAt = DateTime.UtcNow;
            item.CancelledById = userId;
            item.CancelReason = reason;
            item.UpdatedAt = DateTime.UtcNow;
            await SaveOrderAsync(order);
            _logger.LogWarning("Order {OrderId}: item {ItemId} cancelled by user {UserId}: {Reason}", orderId, itemId, userId, reason);
        }

        // ---------- Mesa y comensales ----------

        public async Task MoveToTableAsync(int orderId, int tableId)
        {
            var order = await GetOpenOrderAsync(orderId);
            if (order.TableId == tableId)
                return;

            var target = await GetUsableTableAsync(tableId);
            if (await _orders.HasOpenOrderForTableAsync(tableId))
                throw new InvalidOperationException($"La mesa {target.TableNumber} ya tiene una cuenta abierta");

            order.Table.Status = TableStatus.Available;
            order.Table.UpdatedAt = DateTime.UtcNow;
            target.Status = TableStatus.Occupied;
            target.UpdatedAt = DateTime.UtcNow;
            order.TableId = target.Id;
            order.Table = target;
            await SaveOrderAsync(order);
        }

        public async Task SetGuestsAsync(int orderId, int guests)
        {
            ValidateGuests(guests);
            var order = await GetOpenOrderAsync(orderId);
            order.Guests = guests;
            await SaveOrderAsync(order);
        }

        // ---------- Cuenta y cierre ----------

        public async Task<Bill> GetBillAsync(int orderId)
        {
            var order = await GetAsync(orderId);
            var restaurant = await _restaurants.GetByIdIncludingInactiveAsync(order.RestaurantId)
                ?? throw new KeyNotFoundException("No se encontró tu restaurante");

            var lines = Live(order)
                .Where(i => i.Status is not (OrderItemStatus.Draft or OrderItemStatus.Cancelled))
                .ToList();
            var subtotal = lines.Sum(i => i.LineTotal);
            var tip = RoundMoney(subtotal * restaurant.SuggestedTipPercent / 100m, restaurant.CurrencyCode);
            return new Bill(order, lines, subtotal, restaurant.SuggestedTipPercent, tip, subtotal + tip, restaurant.CurrencyCode);
        }

        public async Task<Order> CloseAsync(int orderId, PaymentMethod paymentMethod, decimal tipAmount, int userId)
        {
            if (tipAmount < 0)
                throw new ArgumentException("La propina no puede ser negativa");
            if (!Enum.IsDefined(paymentMethod))
                throw new ArgumentException("Forma de pago no válida");

            var order = await GetOpenOrderAsync(orderId);
            var items = Live(order).ToList();

            var pending = items.Count(i => InKitchen.Contains(i.Status));
            if (pending > 0)
                throw new InvalidOperationException(
                    $"Quedan {pending} plato(s) sin servir; sírvelos o anúlalos antes de cerrar la cuenta");

            var now = DateTime.UtcNow;
            foreach (var item in items)
            {
                if (item.Status == OrderItemStatus.Draft)
                {
                    // Apuntado pero nunca enviado: no se cobra.
                    item.IsActive = false;
                    item.UpdatedAt = now;
                }
                else if (item.Status == OrderItemStatus.Ready)
                {
                    // Estaba en el pase: si se cobra, se sirvió.
                    ApplyStatus(item, OrderItemStatus.Served, now);
                }
            }

            order.Subtotal = items.Where(i => i.IsActive && i.Status == OrderItemStatus.Served).Sum(i => i.LineTotal);
            order.TipAmount = tipAmount;
            order.PaymentMethod = paymentMethod;
            order.Status = OrderStatus.Closed;
            order.ClosedAt = now;
            order.ClosedById = userId;
            FreeTable(order);

            await SaveOrderAsync(order);
            _logger.LogInformation("Closed order {OrderId}: subtotal {Subtotal}, tip {Tip}", order.Id, order.Subtotal, tipAmount);
            return await GetAsync(order.Id);
        }

        public async Task CancelAsync(int orderId, string reason, int userId)
        {
            reason = RequireReason(reason);
            var order = await GetOpenOrderAsync(orderId);
            var now = DateTime.UtcNow;

            foreach (var item in Live(order).Where(i => i.Status != OrderItemStatus.Cancelled))
            {
                item.Status = OrderItemStatus.Cancelled;
                item.CancelledAt = now;
                item.CancelledById = userId;
                item.CancelReason = reason;
                item.UpdatedAt = now;
            }

            order.Status = OrderStatus.Cancelled;
            order.CancelReason = reason;
            order.ClosedAt = now;
            order.ClosedById = userId;
            order.Subtotal = 0;
            FreeTable(order);

            await SaveOrderAsync(order);
            _logger.LogWarning("Order {OrderId} cancelled by user {UserId}: {Reason}", orderId, userId, reason);
        }

        // ---------- Ayudas ----------

        private static IEnumerable<OrderItem> Live(Order order) => order.Items.Where(i => i.IsActive);

        private async Task<Order> GetOpenOrderAsync(int orderId)
        {
            var order = await GetAsync(orderId);
            EnsureOpen(order);
            return order;
        }

        private static void EnsureOpen(Order order)
        {
            if (order.Status != OrderStatus.Open)
                throw new InvalidOperationException("La cuenta ya está cerrada");
        }

        private async Task<(Order, OrderItem)> GetDraftItemAsync(int orderId, int itemId)
        {
            var order = await GetOpenOrderAsync(orderId);
            var item = Live(order).FirstOrDefault(i => i.Id == itemId)
                ?? throw new KeyNotFoundException($"No se encontró el plato {itemId} en la cuenta {orderId}");
            if (item.Status != OrderItemStatus.Draft)
                throw new InvalidOperationException("El plato ya se envió a cocina; para quitarlo hay que anularlo");
            return (order, item);
        }

        private async Task<Table> GetUsableTableAsync(int tableId)
        {
            var table = await _tables.GetByIdAsync(tableId)
                ?? throw new KeyNotFoundException($"No se encontró la mesa con ID {tableId}");
            if (table.Status == TableStatus.OutOfService)
                throw new InvalidOperationException($"La mesa {table.TableNumber} está fuera de servicio");
            return table;
        }

        private static void FreeTable(Order order)
        {
            if (order.Table.Status == TableStatus.Occupied)
            {
                order.Table.Status = TableStatus.Available;
                order.Table.UpdatedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Guarda tocando también la cuenta, para que su RowVersion detecte a dos camareros
        /// cambiando la misma cuenta a la vez (el segundo recibe un 409 y actualiza).
        /// </summary>
        private async Task SaveOrderAsync(Order order)
        {
            order.UpdatedAt = DateTime.UtcNow;
            await _orders.SaveAsync();
            await NotifyAsync(order);
        }

        private Task NotifyAsync(Order order) => _notifier.OrderChangedAsync(order.RestaurantId, order.Id);

        private Task NotifyReadyAsync(Order order, IEnumerable<OrderItem> items) =>
            _notifier.ItemsReadyAsync(order.RestaurantId, order.Id, order.Table?.TableNumber ?? 0, order.WaiterId,
                items.Select(i => i.Quantity > 1 ? $"{i.Quantity} x {i.Name}" : i.Name).ToList());

        private static OrderItem NewItem(Order order, MenuItem menuItem, int quantity, string? notes,
            OrderCourse? course, Guid? clientRequestId, decimal unitPrice) => new()
        {
            RestaurantId = order.RestaurantId,
            Order = order,
            MenuItemId = menuItem.Id,
            StationId = menuItem.StationId,
            Name = menuItem.Name,
            UnitPrice = unitPrice,
            Quantity = quantity,
            Notes = notes,
            Course = course ?? DefaultCourse(menuItem.Category),
            Status = OrderItemStatus.Draft,
            ClientRequestId = clientRequestId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
        };

        /// <summary>Tiempo en que sale un plato si el camarero no indica otro.</summary>
        public static OrderCourse DefaultCourse(MenuItemCategory category) => category switch
        {
            MenuItemCategory.Appetizer => OrderCourse.Starter,
            MenuItemCategory.MainCourse or MenuItemCategory.SideDish => OrderCourse.Main,
            MenuItemCategory.Dessert => OrderCourse.Dessert,
            _ => OrderCourse.Immediate, // bebidas
        };

        private static void ApplyStatus(OrderItem item, OrderItemStatus status, DateTime now)
        {
            item.Status = status;
            item.UpdatedAt = now;
            switch (status)
            {
                case OrderItemStatus.Sent:
                    item.StartedAt = null;
                    break;
                case OrderItemStatus.Preparing:
                    item.StartedAt ??= now;
                    item.ReadyAt = null;
                    break;
                case OrderItemStatus.Ready:
                    item.ReadyAt = now;
                    item.ServedAt = null;
                    break;
                case OrderItemStatus.Served:
                    item.ReadyAt ??= now;
                    item.ServedAt = now;
                    break;
            }
        }

        /// <summary>El peso colombiano no usa centavos en la práctica: se redondea a pesos enteros.</summary>
        public static decimal RoundMoney(decimal amount, string currencyCode) =>
            Math.Round(amount, currencyCode == "COP" ? 0 : 2, MidpointRounding.AwayFromZero);

        private static void ValidateGuests(int guests)
        {
            if (guests < 1 || guests > MaxGuests)
                throw new ArgumentException($"El número de comensales debe estar entre 1 y {MaxGuests}");
        }

        private static void ValidateQuantity(int quantity)
        {
            if (quantity < 1 || quantity > MaxQuantity)
                throw new ArgumentException($"La cantidad debe estar entre 1 y {MaxQuantity}");
        }

        private static string? CleanNotes(string? notes, int max)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return null;
            notes = notes.Trim();
            if (notes.Length > max)
                throw new ArgumentException($"Las notas no pueden superar {max} caracteres");
            return notes;
        }

        private static string RequireReason(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Indica el motivo de la anulación");
            reason = reason.Trim();
            if (reason.Length > 200)
                throw new ArgumentException("El motivo no puede superar 200 caracteres");
            return reason;
        }
    }

    /// <summary>Sin avisos (procesos sin pantallas conectadas).</summary>
    public class NoOpOrderNotifier : IOrderNotifier
    {
        public Task OrderChangedAsync(int restaurantId, int orderId) => Task.CompletedTask;

        public Task ItemsReadyAsync(int restaurantId, int orderId, int tableNumber, int waiterId, IReadOnlyList<string> items) =>
            Task.CompletedTask;
    }
}

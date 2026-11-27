using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Repositories;

namespace SistemaRestaurante.DataAccess.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private static readonly OrderItemStatus[] KitchenStatuses =
            { OrderItemStatus.Held, OrderItemStatus.Sent, OrderItemStatus.Preparing, OrderItemStatus.Ready };

        private readonly RestaurantDbContext _context;

        public OrderRepository(RestaurantDbContext context)
        {
            _context = context;
        }

        private IQueryable<Order> WithDetail => _context.Orders
            .Where(o => o.IsActive)
            .Include(o => o.Table)
            .Include(o => o.Waiter)
            .Include(o => o.Items.Where(i => i.IsActive).OrderBy(i => i.Course).ThenBy(i => i.Id))
                .ThenInclude(i => i.Station)
            .AsSplitQuery();

        public Task<Order?> GetDetailAsync(int id) => WithDetail.FirstOrDefaultAsync(o => o.Id == id);

        public async Task<IEnumerable<Order>> GetOpenAsync() =>
            await WithDetail.Where(o => o.Status == OrderStatus.Open)
                .OrderBy(o => o.Table.TableNumber)
                .ToListAsync();

        public Task<bool> HasOpenOrderForTableAsync(int tableId) =>
            _context.Orders.AnyAsync(o => o.IsActive && o.TableId == tableId && o.Status == OrderStatus.Open);

        public async Task<IEnumerable<Order>> GetClosedBetweenAsync(DateTime fromUtc, DateTime toUtc) =>
            await WithDetail
                .Where(o => o.Status != OrderStatus.Open && o.ClosedAt >= fromUtc && o.ClosedAt < toUtc)
                .OrderBy(o => o.ClosedAt)
                .ToListAsync();

        public async Task<IEnumerable<OrderItem>> GetKitchenItemsAsync(int? stationId)
        {
            var query = _context.OrderItems
                .Where(i => i.IsActive && KitchenStatuses.Contains(i.Status)
                    && i.Order.IsActive && i.Order.Status == OrderStatus.Open);
            if (stationId is int id)
                query = query.Where(i => i.StationId == id);

            return await query
                .Include(i => i.Order).ThenInclude(o => o.Table)
                .Include(i => i.Order).ThenInclude(o => o.Waiter)
                .Include(i => i.Station)
                .OrderBy(i => i.SentAt ?? i.CreatedAt).ThenBy(i => i.OrderId).ThenBy(i => i.Course).ThenBy(i => i.Id)
                .AsSplitQuery()
                .ToListAsync();
        }

        public Task<OrderItem?> GetItemAsync(int orderId, int itemId) =>
            _context.OrderItems
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.IsActive && i.Id == itemId && i.OrderId == orderId && i.Order.IsActive);

        public async Task<Order> CreateAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
            await SaveAsync();
            return order;
        }

        public async Task SaveAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException(
                    "Otro dispositivo cambió esta cuenta al mismo tiempo. Actualiza y vuelve a intentarlo");
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 } sql)
            {
                if (sql.Message.Contains("IX_Orders_OneOpenPerTable"))
                    throw new InvalidOperationException("La mesa ya tiene una cuenta abierta");
                throw new InvalidOperationException("Este plato ya se había añadido (envío repetido)");
            }
        }
    }
}

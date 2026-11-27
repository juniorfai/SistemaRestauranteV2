using Microsoft.EntityFrameworkCore;
using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces.Repositories;

namespace SistemaRestaurante.DataAccess.Repositories
{
    public class StationRepository : GenericRepository<Station>, IStationRepository
    {
        public StationRepository(RestaurantDbContext context) : base(context)
        {
        }

        public async Task<Station?> GetFirstOfTypeAsync(StationType type) =>
            await Active.Where(s => s.Type == type).OrderBy(s => s.SortOrder).ThenBy(s => s.Id).FirstOrDefaultAsync();

        public Task<int> CountMenuItemsAsync(int stationId) =>
            _context.MenuItems.CountAsync(m => m.IsActive && m.StationId == stationId);

        public Task<int> CountAsync() => Active.CountAsync();

        public async Task CreateManyAsync(IEnumerable<Station> stations)
        {
            foreach (var station in stations)
            {
                station.CreatedAt = DateTime.UtcNow;
                station.IsActive = true;
            }
            await _dbSet.AddRangeAsync(stations);
            await _context.SaveChangesAsync();
        }
    }
}

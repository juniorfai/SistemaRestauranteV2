using Microsoft.EntityFrameworkCore;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Interfaces;

namespace SistemaRestaurante.DataAccess.Context
{
    /// <summary>
    /// Base de datos compartida por todos los restaurantes. Cada consulta se filtra sola por el
    /// restaurante del usuario (filtros globales de EF Core), así un restaurante nunca ve ni modifica
    /// los datos de otro. Fuera de una petición (arranque, datos de ejemplo) no hay restaurante y
    /// esas consultas deben usar IgnoreQueryFilters().
    /// </summary>
    public class RestaurantDbContext : DbContext
    {
        private readonly ITenantContext? _tenant;

        public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options, ITenantContext? tenant = null)
            : base(options)
        {
            _tenant = tenant;
        }

        // Los filtros globales leen estas propiedades en cada consulta.
        public int? CurrentRestaurantId => _tenant?.RestaurantId;
        public bool IsPlatformAdmin => _tenant?.IsPlatformAdmin ?? false;

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<ReservationMenuItem> ReservationMenuItems { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de ReservationMenuItem (Relación N:M)
            modelBuilder.Entity<ReservationMenuItem>()
                .HasKey(rm => new { rm.ReservationId, rm.MenuItemId });

            modelBuilder.Entity<ReservationMenuItem>()
                .HasOne(rm => rm.Reservation)
                .WithMany(r => r.ReservationMenuItems)
                .HasForeignKey(rm => rm.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReservationMenuItem>()
                .HasOne(rm => rm.MenuItem)
                .WithMany(m => m.ReservationMenuItems)
                .HasForeignKey(rm => rm.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReservationMenuItem>()
                .Property(rm => rm.UnitPrice)
                .HasPrecision(18, 2);

            // El email del cliente es único dentro de cada restaurante.
            modelBuilder.Entity<Customer>()
                .HasIndex(c => new { c.RestaurantId, c.Email })
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasOne<Restaurant>()
                .WithMany()
                .HasForeignKey(c => c.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Table>()
                .HasIndex(t => new { t.RestaurantId, t.TableNumber })
                .IsUnique();

            modelBuilder.Entity<MenuItem>()
                .Property(m => m.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Reservation>()
                .Property(r => r.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Reservation>()
                .HasOne<Restaurant>()
                .WithMany()
                .HasForeignKey(r => r.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => new { r.RestaurantId, r.ReservationDateTime });

            // Usuarios del personal
            modelBuilder.Entity<User>(user =>
            {
                user.HasIndex(u => u.Username).IsUnique();
                user.Property(u => u.Username).HasMaxLength(50).IsRequired();
                user.Property(u => u.FullName).HasMaxLength(100).IsRequired();
                user.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
                user.Property(u => u.SecurityStamp).HasMaxLength(64).IsRequired();
                user.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
                user.HasOne(u => u.Restaurant)
                    .WithMany()
                    .HasForeignKey(u => u.RestaurantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- Aislamiento por restaurante ----------
            modelBuilder.Entity<Table>().HasQueryFilter(e => e.RestaurantId == CurrentRestaurantId);
            modelBuilder.Entity<MenuItem>().HasQueryFilter(e => e.RestaurantId == CurrentRestaurantId);
            modelBuilder.Entity<Customer>().HasQueryFilter(e => e.RestaurantId == CurrentRestaurantId);
            modelBuilder.Entity<Reservation>().HasQueryFilter(e => e.RestaurantId == CurrentRestaurantId);
            modelBuilder.Entity<ReservationMenuItem>().HasQueryFilter(rm => rm.Reservation.RestaurantId == CurrentRestaurantId);

            // El administrador de la plataforma ve todos los restaurantes y usuarios; el resto, solo el suyo.
            modelBuilder.Entity<Restaurant>().HasQueryFilter(r => IsPlatformAdmin || r.Id == CurrentRestaurantId);
            modelBuilder.Entity<User>().HasQueryFilter(u => IsPlatformAdmin || u.RestaurantId == CurrentRestaurantId);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ApplyRestaurantScope();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ApplyRestaurantScope();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>
        /// Segunda barrera tras los filtros: lo nuevo se asigna al restaurante del usuario y
        /// nada puede guardarse en otro restaurante ni cambiarse de restaurante.
        /// </summary>
        private void ApplyRestaurantScope()
        {
            var current = CurrentRestaurantId;

            foreach (var entry in ChangeTracker.Entries<IRestaurantScoped>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.RestaurantId == 0)
                    {
                        entry.Entity.RestaurantId = current
                            ?? throw new InvalidOperationException("No se puede guardar sin un restaurante");
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    var property = entry.Property(e => e.RestaurantId);
                    if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                        throw new InvalidOperationException("No se puede cambiar el restaurante de un registro");
                }

                var isWrite = entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;
                if (current != null && isWrite && entry.Entity.RestaurantId != current)
                    throw new ForbiddenAccessException("No tienes acceso a los datos de otro restaurante");
            }
        }
    }
}

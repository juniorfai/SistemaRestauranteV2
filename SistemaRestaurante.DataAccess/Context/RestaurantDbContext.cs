using Microsoft.EntityFrameworkCore;
using SistemaRestaurante.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRestaurante.DataAccess.Context
{
    public class RestaurantDbContext : DbContext
    {
        public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options)
            : base(options)
        {
        }

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

            // Constraints y configuraciones adicionales
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Email)
                .IsUnique();

            modelBuilder.Entity<Table>()
                .HasIndex(t => new { t.RestaurantId, t.TableNumber })
                .IsUnique();

            modelBuilder.Entity<MenuItem>()
                .Property(m => m.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Reservation>()
                .Property(r => r.TotalAmount)
                .HasPrecision(18, 2);

            // Usuarios del personal
            modelBuilder.Entity<User>(user =>
            {
                user.HasIndex(u => u.Username).IsUnique();
                user.Property(u => u.Username).HasMaxLength(50).IsRequired();
                user.Property(u => u.FullName).HasMaxLength(100).IsRequired();
                user.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
                user.Property(u => u.SecurityStamp).HasMaxLength(64).IsRequired();
                user.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            });
        }
    }
}

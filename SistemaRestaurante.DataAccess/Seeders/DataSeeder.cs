using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.DataAccess.Seeders
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(RestaurantDbContext context)
        {
            // Verificar si ya hay datos
            if (await context.Customers.AnyAsync())
                return;

            // 1. Crear Restaurante
            var restaurant = new Restaurant
            {
                Name = "Gourmet Paradise",
                Address = "Calle Principal #123, Ciudad",
                PhoneNumber = "+1234567890",
                Email = "contact@gourmetparadise.com",
                OpeningHours = "Lun-Dom: 12:00 - 23:00",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            await context.Restaurants.AddAsync(restaurant);
            await context.SaveChangesAsync();

            // 2. Crear Mesas
            var tables = new List<Table>();
            for (int i = 1; i <= 15; i++)
            {
                int capacity = i <= 5 ? 2 : (i <= 10 ? 4 : 6);
                tables.Add(new Table
                {
                    TableNumber = i,
                    Capacity = capacity,
                    Location = i <= 5 ? "Ventana" : (i <= 10 ? "Centro" : "Terraza"),
                    Status = TableStatus.Available,
                    RestaurantId = restaurant.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }
            await context.Tables.AddRangeAsync(tables);
            await context.SaveChangesAsync();

            // 3. Crear Menú
            var menuItems = new List<MenuItem>
            {
                new MenuItem { Name = "Ensalada César", Description = "Lechuga romana, crutones, parmesano", Price = 8.99m, Category = MenuItemCategory.Appetizer, RestaurantId = restaurant.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Bruschetta", Description = "Pan tostado con tomate y albahaca", Price = 6.99m, Category = MenuItemCategory.Appetizer, RestaurantId = restaurant.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Filete Mignon", Description = "Filete con salsa de vino tinto", Price = 28.99m, Category = MenuItemCategory.MainCourse, RestaurantId = restaurant.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Salmón a la Parrilla", Description = "Salmón con vegetales", Price = 22.99m, Category = MenuItemCategory.MainCourse, RestaurantId = restaurant.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Pasta Alfredo", Description = "Pasta con salsa cremosa", Price = 15.99m, Category = MenuItemCategory.MainCourse, RestaurantId = restaurant.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Tarta de Queso", Description = "Con salsa de frutos rojos", Price = 7.99m, Category = MenuItemCategory.Dessert, RestaurantId = restaurant.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Brownie", Description = "Con helado de vainilla", Price = 6.99m, Category = MenuItemCategory.Dessert, RestaurantId = restaurant.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Vino Tinto", Description = "Copa de vino reserva", Price = 9.99m, Category = MenuItemCategory.Drink, RestaurantId = restaurant.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Refresco", Description = "Gaseosa 500ml", Price = 3.99m, Category = MenuItemCategory.Drink, RestaurantId = restaurant.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow }
            };
            await context.MenuItems.AddRangeAsync(menuItems);
            await context.SaveChangesAsync();

            // 4. Crear Clientes
            var customers = new List<Customer>
            {
                new Customer { FirstName = "Juan", LastName = "Pérez", Email = "juan@email.com", PhoneNumber = "555-0101", Preferences = "Mesa cerca de ventana", CreatedAt = DateTime.UtcNow, IsActive = true },
                new Customer { FirstName = "María", LastName = "García", Email = "maria@email.com", PhoneNumber = "555-0102", Preferences = "Sin cerdo", CreatedAt = DateTime.UtcNow, IsActive = true },
                new Customer { FirstName = "Carlos", LastName = "López", Email = "carlos@email.com", PhoneNumber = "555-0103", Preferences = "Alergia al maní", CreatedAt = DateTime.UtcNow, IsActive = true },
                new Customer { FirstName = "Ana", LastName = "Martínez", Email = "ana@email.com", PhoneNumber = "555-0104", Preferences = "", CreatedAt = DateTime.UtcNow, IsActive = true }
            };
            await context.Customers.AddRangeAsync(customers);
            await context.SaveChangesAsync();

            // 5. Crear Reservas
            var reservations = new List<Reservation>();
            var today = DateTime.Today.AddDays(1).AddHours(20); // 8 PM, desde mañana (hora local del restaurante)

            for (int i = 0; i < customers.Count; i++)
            {
                var table = tables[i % tables.Count];
                reservations.Add(new Reservation
                {
                    ReservationDateTime = today.AddDays(i),
                    NumberOfPeople = table.Capacity,
                    Status = i == 0 ? ReservationStatus.Confirmed : ReservationStatus.Pending,
                    SpecialRequests = i == 1 ? "Aniversario" : null,
                    CustomerId = customers[i].Id,
                    TableId = table.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }
            await context.Reservations.AddRangeAsync(reservations);
            await context.SaveChangesAsync();
        }
    
        /// <summary>
        /// Usuarios de prueba para desarrollo (uno por rol, además del administrador inicial).
        /// Solo se ejecuta con Database:SeedDemoData = true. Nunca activar en producción.
        /// </summary>
        public static async Task SeedDemoUsersAsync(RestaurantDbContext context, IUserService userService)
        {
            if (await context.Users.AnyAsync(u => u.Role != UserRole.Admin))
                return;

            await userService.CreateAsync("encargado", "Laura Encargada", UserRole.Manager, "Encargado123!");
            await userService.CreateAsync("camarero", "Pablo Camarero", UserRole.Staff, "Camarero123!");
        }
    }
}

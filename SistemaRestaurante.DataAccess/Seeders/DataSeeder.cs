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
            // Verificar si ya hay datos (sin usuario conectado, hay que saltarse el filtro por restaurante)
            if (await context.Customers.IgnoreQueryFilters().AnyAsync())
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

            // 3. Zonas de preparación: cada plato sale por la pantalla (y la impresora) de su zona.
            var kitchen = new Station { Name = "Cocina", Type = StationType.Kitchen, SortOrder = 1, RestaurantId = restaurant.Id, CreatedAt = DateTime.UtcNow, IsActive = true };
            var bar = new Station { Name = "Barra", Type = StationType.Bar, SortOrder = 2, RestaurantId = restaurant.Id, CreatedAt = DateTime.UtcNow, IsActive = true };
            await context.Stations.AddRangeAsync(kitchen, bar);
            await context.SaveChangesAsync();

            // 4. Crear Menú (precios en pesos colombianos)
            var menuItems = new List<MenuItem>
            {
                new MenuItem { Name = "Ensalada César", Description = "Lechuga romana, crutones, parmesano", Price = 24000m, Category = MenuItemCategory.Appetizer, RestaurantId = restaurant.Id, StationId = kitchen.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Bruschetta", Description = "Pan tostado con tomate y albahaca", Price = 18000m, Category = MenuItemCategory.Appetizer, RestaurantId = restaurant.Id, StationId = kitchen.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Filete Mignon", Description = "Filete con salsa de vino tinto", Price = 68000m, Category = MenuItemCategory.MainCourse, RestaurantId = restaurant.Id, StationId = kitchen.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Salmón a la Parrilla", Description = "Salmón con vegetales", Price = 52000m, Category = MenuItemCategory.MainCourse, RestaurantId = restaurant.Id, StationId = kitchen.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Pasta Alfredo", Description = "Pasta con salsa cremosa", Price = 36000m, Category = MenuItemCategory.MainCourse, RestaurantId = restaurant.Id, StationId = kitchen.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Tarta de Queso", Description = "Con salsa de frutos rojos", Price = 16000m, Category = MenuItemCategory.Dessert, RestaurantId = restaurant.Id, StationId = kitchen.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Brownie", Description = "Con helado de vainilla", Price = 15000m, Category = MenuItemCategory.Dessert, RestaurantId = restaurant.Id, StationId = kitchen.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Vino Tinto", Description = "Copa de vino reserva", Price = 22000m, Category = MenuItemCategory.Drink, RestaurantId = restaurant.Id, StationId = bar.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow },
                new MenuItem { Name = "Refresco", Description = "Gaseosa 500ml", Price = 6000m, Category = MenuItemCategory.Drink, RestaurantId = restaurant.Id, StationId = bar.Id, IsAvailable = true, CreatedAt = DateTime.UtcNow }
            };
            await context.MenuItems.AddRangeAsync(menuItems);
            await context.SaveChangesAsync();

            // 5. Crear Clientes
            var customers = new List<Customer>
            {
                new Customer { FirstName = "Juan", RestaurantId = restaurant.Id, LastName = "Pérez", Email = "juan@email.com", PhoneNumber = "555-0101", Preferences = "Mesa cerca de ventana", CreatedAt = DateTime.UtcNow, IsActive = true },
                new Customer { FirstName = "María", RestaurantId = restaurant.Id, LastName = "García", Email = "maria@email.com", PhoneNumber = "555-0102", Preferences = "Sin cerdo", CreatedAt = DateTime.UtcNow, IsActive = true },
                new Customer { FirstName = "Carlos", RestaurantId = restaurant.Id, LastName = "López", Email = "carlos@email.com", PhoneNumber = "555-0103", Preferences = "Alergia al maní", CreatedAt = DateTime.UtcNow, IsActive = true },
                new Customer { FirstName = "Ana", RestaurantId = restaurant.Id, LastName = "Martínez", Email = "ana@email.com", PhoneNumber = "555-0104", Preferences = "", CreatedAt = DateTime.UtcNow, IsActive = true }
            };
            await context.Customers.AddRangeAsync(customers);
            await context.SaveChangesAsync();

            // 6. Crear Reservas
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
                    RestaurantId = restaurant.Id,
                    TableId = table.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }
            await context.Reservations.AddRangeAsync(reservations);
            await context.SaveChangesAsync();
        }
    
        /// <summary>
        /// Personal de prueba del primer restaurante, uno por rol. Solo se ejecuta con
        /// Database:SeedDemoData = true. Nunca activar en producción.
        /// </summary>
        public static async Task SeedDemoUsersAsync(RestaurantDbContext context, IUserService userService)
        {
            var restaurant = await context.Restaurants.IgnoreQueryFilters().OrderBy(r => r.Id).FirstOrDefaultAsync();
            if (restaurant == null || await context.Users.IgnoreQueryFilters().AnyAsync(u => u.RestaurantId == restaurant.Id))
                return;

            await userService.CreateForRestaurantAsync(restaurant.Id, "admin", "Administrador", UserRole.Admin, "Admin123!");
            await userService.CreateForRestaurantAsync(restaurant.Id, "encargado", "Laura Encargada", UserRole.Manager, "Encargado123!");
            await userService.CreateForRestaurantAsync(restaurant.Id, "camarero", "Pablo Camarero", UserRole.Staff, "Camarero123!");
            await userService.CreateForRestaurantAsync(restaurant.Id, "cocina", "Pantalla de Cocina", UserRole.Kitchen, "Cocina123!");
        }
    }
}

using System.Net;
using SistemaRestaurante.Tests.Infrastructure;

namespace SistemaRestaurante.Tests
{
    /// <summary>Lo que hace (y no hace) el administrador de la plataforma.</summary>
    public class PlatformAdminTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _api;

        public PlatformAdminTests(ApiFactory api)
        {
            _api = api;
        }

        [Fact]
        public async Task Creates_restaurant_with_its_first_admin()
        {
            var restaurant = await _api.CreateRestaurantAsync("Casa Nueva");

            var platform = await _api.PlatformAsync();
            var all = await platform.GetOkAsync<List<RestaurantDto>>("/api/Restaurant");
            Assert.Contains(all, r => r.Id == restaurant.Id && r.Name == "Casa Nueva" && r.IsActive);
            Assert.Equal("Admin", restaurant.Admin.Session!.User.Role);
            Assert.Equal(restaurant.Id, restaurant.Admin.Session.User.RestaurantId);
        }

        [Fact]
        public async Task Failed_creation_leaves_no_half_created_restaurant()
        {
            var existing = await _api.CreateRestaurantAsync();
            var platform = await _api.PlatformAsync();
            var before = (await platform.GetOkAsync<List<RestaurantDto>>("/api/Restaurant")).Count;

            await HttpAssert.StatusAsync(HttpStatusCode.Conflict, platform.PostAsync("/api/Restaurant", new
            {
                name = "Duplicado", address = "C/ 1", phoneNumber = "600000000", email = "d@pruebas.test",
                adminUsername = existing.AdminUsername, adminFullName = "Repetido", adminPassword = "Repetido-123!",
            }));

            var after = await platform.GetOkAsync<List<RestaurantDto>>("/api/Restaurant");
            Assert.Equal(before, after.Count);
            Assert.DoesNotContain(after, r => r.Name == "Duplicado");
        }

        [Fact]
        public async Task Invalid_restaurant_data_returns_400()
        {
            var platform = await _api.PlatformAsync();
            await HttpAssert.StatusAsync(HttpStatusCode.BadRequest, platform.PostAsync("/api/Restaurant", new
            {
                name = "", address = "C/ 1", phoneNumber = "abc", email = "no-es-un-email",
                adminUsername = "x", adminFullName = "", adminPassword = "corta",
            }));
        }

        [Theory]
        [InlineData("/api/Table")]
        [InlineData("/api/Customer")]
        [InlineData("/api/Reservation")]
        [InlineData("/api/MenuItem")]
        [InlineData("/api/Restaurant/current")]
        public async Task Cannot_see_restaurant_operations(string url)
        {
            var platform = await _api.PlatformAsync();
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, platform.GetAsync(url));
        }

        [Fact]
        public async Task Sees_all_staff_and_can_reset_passwords_but_not_manage_them()
        {
            var restaurant = await _api.CreateRestaurantAsync("Soporte");
            var platform = await _api.PlatformAsync();

            var users = await platform.GetOkAsync<List<UserDto>>("/api/User");
            var admin = Assert.Single(users, u => u.Username == restaurant.AdminUsername);
            Assert.Equal("Soporte", admin.RestaurantName);

            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, platform.PostAsync($"/api/User/{admin.Id}/reset-password",
                new { newPassword = "Soporte-Nueva-1!" }));
            await _api.LoginAsync(restaurant.AdminUsername, "Soporte-Nueva-1!");

            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, platform.PostAsync("/api/User",
                new { username = "creadoporplataforma", fullName = "X", role = "Staff", password = "Plataforma-1!" }));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, platform.PutAsync($"/api/User/{admin.Id}",
                new { fullName = "X", role = "Staff", isActive = false }));
        }

        [Fact]
        public async Task Suspending_a_restaurant_blocks_its_staff_until_reactivated()
        {
            var suspended = await _api.CreateRestaurantAsync();
            var unaffected = await _api.CreateRestaurantAsync();
            var platform = await _api.PlatformAsync();

            await HttpAssert.StatusAsync(HttpStatusCode.NoContent,
                platform.PatchAsync($"/api/Restaurant/{suspended.Id}/active", new { isActive = false }));

            await HttpAssert.StatusAsync(HttpStatusCode.Unauthorized, suspended.Admin.GetAsync("/api/Table"));
            await HttpAssert.StatusAsync(HttpStatusCode.Forbidden, _api.Anonymous().PostAsync("/api/Auth/login",
                new { username = suspended.AdminUsername, password = suspended.AdminPassword }));
            await HttpAssert.StatusAsync(HttpStatusCode.OK, unaffected.Admin.GetAsync("/api/Table"));

            await HttpAssert.StatusAsync(HttpStatusCode.NoContent,
                platform.PatchAsync($"/api/Restaurant/{suspended.Id}/active", new { isActive = true }));
            var again = await _api.LoginAsync(suspended.AdminUsername, suspended.AdminPassword);
            await HttpAssert.StatusAsync(HttpStatusCode.OK, again.GetAsync("/api/Table"));
        }
    }
}

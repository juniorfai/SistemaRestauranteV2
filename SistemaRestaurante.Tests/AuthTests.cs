using System.Net;
using SistemaRestaurante.Tests.Infrastructure;

namespace SistemaRestaurante.Tests
{
    public class AuthTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _api;

        public AuthTests(ApiFactory api)
        {
            _api = api;
        }

        [Fact]
        public async Task Platform_admin_is_created_on_startup_without_restaurant()
        {
            var platform = await _api.PlatformAsync();

            Assert.Equal("PlatformAdmin", platform.Session!.User.Role);
            Assert.Null(platform.Session.User.RestaurantId);
        }

        [Fact]
        public async Task Login_with_wrong_password_returns_401()
        {
            var anonymous = _api.Anonymous();
            await HttpAssert.StatusAsync(HttpStatusCode.Unauthorized,
                anonymous.PostAsync("/api/Auth/login", new { username = ApiFactory.PlatformUser, password = "mala-contraseña" }));
        }

        [Fact]
        public async Task Login_of_restaurant_user_carries_its_restaurant()
        {
            var restaurant = await _api.CreateRestaurantAsync("La Prueba");

            var me = await restaurant.Admin.GetOkAsync<UserDto>("/api/Auth/me");
            Assert.Equal("Admin", me.Role);
            Assert.Equal(restaurant.Id, me.RestaurantId);
            Assert.Equal("La Prueba", me.RestaurantName);
        }

        [Theory]
        [InlineData("/api/Table")]
        [InlineData("/api/Customer")]
        [InlineData("/api/Reservation")]
        [InlineData("/api/MenuItem")]
        [InlineData("/api/User")]
        [InlineData("/api/Restaurant")]
        [InlineData("/api/Auth/me")]
        public async Task Endpoints_require_a_session(string url)
        {
            await HttpAssert.StatusAsync(HttpStatusCode.Unauthorized, _api.Anonymous().GetAsync(url));
        }

        [Fact]
        public async Task Health_check_is_public_and_checks_the_database()
        {
            using var response = await _api.Anonymous().GetAsync("/api/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Forged_token_is_rejected()
        {
            var client = _api.Anonymous();
            client.Http.DefaultRequestHeaders.Authorization = new("Bearer", "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.firma-falsa");
            await HttpAssert.StatusAsync(HttpStatusCode.Unauthorized, client.GetAsync("/api/Auth/me"));
        }

        [Fact]
        public async Task Changing_password_closes_open_sessions()
        {
            var restaurant = await _api.CreateRestaurantAsync();
            var otherDevice = await _api.LoginAsync(restaurant.AdminUsername, restaurant.AdminPassword);

            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, restaurant.Admin.PostAsync("/api/Auth/change-password",
                new { currentPassword = restaurant.AdminPassword, newPassword = "Nueva-Clave-1!" }));

            await HttpAssert.StatusAsync(HttpStatusCode.Unauthorized, otherDevice.GetAsync("/api/Auth/me"));
            await HttpAssert.StatusAsync(HttpStatusCode.Unauthorized,
                _api.Anonymous().PostAsync("/api/Auth/login", new { username = restaurant.AdminUsername, password = restaurant.AdminPassword }));
            await _api.LoginAsync(restaurant.AdminUsername, "Nueva-Clave-1!");
        }

        [Fact]
        public async Task Deactivated_user_cannot_log_in_and_loses_its_session()
        {
            var restaurant = await _api.CreateRestaurantAsync();
            var waiter = await restaurant.AddStaffAsync("Staff");
            var waiterUser = waiter.Session!.User;

            await HttpAssert.StatusAsync(HttpStatusCode.NoContent, restaurant.Admin.PutAsync($"/api/User/{waiterUser.Id}",
                new { fullName = waiterUser.FullName, role = "Staff", isActive = false }));

            await HttpAssert.StatusAsync(HttpStatusCode.Unauthorized, waiter.GetAsync("/api/Table"));
        }

        [Fact]
        public async Task Admin_cannot_deactivate_or_demote_the_last_admin()
        {
            var restaurant = await _api.CreateRestaurantAsync();
            var me = restaurant.Admin.Session!.User;

            var status = await restaurant.Admin.StatusOfAsync(restaurant.Admin.PutAsync($"/api/User/{me.Id}",
                new { fullName = me.FullName, role = "Staff", isActive = true }));
            Assert.True(status is HttpStatusCode.BadRequest or HttpStatusCode.Conflict, $"Llegó {(int)status}");
            await HttpAssert.StatusAsync(HttpStatusCode.OK, restaurant.Admin.GetAsync("/api/User"));
        }
    }

    /// <summary>Con el límite real de intentos de login, el siguiente intento recibe 429.</summary>
    public class LoginRateLimitTests : IClassFixture<StrictLoginApiFactory>
    {
        private readonly StrictLoginApiFactory _api;

        public LoginRateLimitTests(StrictLoginApiFactory api)
        {
            _api = api;
        }

        private ApiClient ClientFrom(string ip)
        {
            var client = _api.Anonymous();
            client.Http.DefaultRequestHeaders.Add("X-Forwarded-For", ip);
            return client;
        }

        [Fact]
        public async Task Too_many_login_attempts_from_one_ip_return_429_without_blocking_others()
        {
            var attacker = ClientFrom("203.0.113.10");
            var body = new { username = "nadie", password = "nada" };
            for (var i = 0; i < StrictLoginApiFactory.Limit; i++)
                await HttpAssert.StatusAsync(HttpStatusCode.Unauthorized, attacker.PostAsync("/api/Auth/login", body));

            await HttpAssert.StatusAsync(HttpStatusCode.TooManyRequests, attacker.PostAsync("/api/Auth/login", body));

            // Detrás del proxy cada cliente tiene su propio límite: otro restaurante sigue pudiendo entrar.
            await HttpAssert.StatusAsync(HttpStatusCode.Unauthorized, ClientFrom("198.51.100.20").PostAsync("/api/Auth/login", body));
        }

        [Fact]
        public async Task Spoofed_forwarded_header_does_not_bypass_the_limit()
        {
            // El proxy añade la IP real al final; las que mande el cliente delante se ignoran.
            var body = new { username = "nadie", password = "nada" };
            for (var i = 0; i < StrictLoginApiFactory.Limit; i++)
                await HttpAssert.StatusAsync(HttpStatusCode.Unauthorized,
                    ClientFrom($"10.0.0.{i}, 192.0.2.50").PostAsync("/api/Auth/login", body));

            await HttpAssert.StatusAsync(HttpStatusCode.TooManyRequests,
                ClientFrom("10.0.0.99, 192.0.2.50").PostAsync("/api/Auth/login", body));
        }
    }
}

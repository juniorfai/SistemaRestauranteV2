using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaRestaurante.DataAccess.Context;

namespace SistemaRestaurante.Tests.Infrastructure
{
    /// <summary>
    /// Levanta la API completa en memoria contra una base de SQL Server propia y vacía, creada con
    /// las migraciones reales y borrada al terminar. Nunca usa la base de desarrollo.
    ///
    /// Servidor: variable de entorno TEST_SQLSERVER (cadena de conexión sin base de datos);
    /// por defecto, LocalDB.
    /// </summary>
    public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public const string PlatformUser = "plataforma";
        public const string PlatformPassword = "Plataforma-Test-1!";

        private const string DefaultServer = @"Server=(localdb)\mssqllocaldb;Trusted_Connection=True;MultipleActiveResultSets=true";

        public string ConnectionString { get; }

        /// <summary>Intentos de login por minuto; alto para no molestar al resto de pruebas.</summary>
        protected virtual int LoginAttemptsPerMinute => 10_000;

        /// <summary>Simula estar detrás del proxy inverso (cabeceras X-Forwarded-*).</summary>
        protected virtual bool BehindProxy => false;

        public ApiFactory()
        {
            var server = Environment.GetEnvironmentVariable("TEST_SQLSERVER");
            var builder = new SqlConnectionStringBuilder(string.IsNullOrWhiteSpace(server) ? DefaultServer : server)
            {
                InitialCatalog = $"SistemaRestaurante_Test_{Guid.NewGuid():N}",
                MultipleActiveResultSets = true,
            };
            ConnectionString = builder.ConnectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Entorno propio: no se carga appsettings.Development.json (ni su base ni sus datos de ejemplo).
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
            builder.UseSetting("Jwt:Key", "clave-solo-para-pruebas-automaticas-0123456789");
            builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
            builder.UseSetting("Database:SeedDemoData", "false");
            builder.UseSetting("Auth:PlatformAdmin:Username", PlatformUser);
            builder.UseSetting("Auth:PlatformAdmin:FullName", "Plataforma (pruebas)");
            builder.UseSetting("Auth:PlatformAdmin:Password", PlatformPassword);
            builder.UseSetting("Auth:LoginAttemptsPerMinute", LoginAttemptsPerMinute.ToString());
            builder.UseSetting("ReverseProxy:Enabled", BehindProxy ? "true" : "false");
        }

        /// <summary>Cliente sin sesión.</summary>
        public ApiClient Anonymous() => new(CreateClient());

        /// <summary>Inicia sesión y devuelve un cliente con el token puesto.</summary>
        public async Task<ApiClient> LoginAsync(string username, string password)
        {
            var client = Anonymous();
            await client.LoginAsync(username, password);
            return client;
        }

        public Task<ApiClient> PlatformAsync() => LoginAsync(PlatformUser, PlatformPassword);

        /// <summary>
        /// Da de alta un restaurante nuevo (como la plataforma) y devuelve la sesión de su administrador.
        /// </summary>
        public async Task<TestRestaurant> CreateRestaurantAsync(string? name = null)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var admin = $"admin{suffix}";
            const string password = "Admin-Test-1!";
            var platform = await PlatformAsync();
            var created = await platform.PostOkAsync<RestaurantDto>("/api/Restaurant", new
            {
                name = name ?? $"Restaurante {suffix}",
                address = "Calle de las pruebas 1",
                phoneNumber = "600000000",
                email = $"r{suffix}@pruebas.test",
                openingHours = "13-23",
                adminUsername = admin,
                adminFullName = "Admin de pruebas",
                adminPassword = password,
            });
            var adminClient = await LoginAsync(admin, password);
            return new TestRestaurant(this, created.Id, created.Name, admin, password, adminClient);
        }

        /// <summary>Acceso directo a la base, para preparar o comprobar datos.</summary>
        public async Task WithDbAsync(Func<RestaurantDbContext, Task> action)
        {
            using var scope = Services.CreateScope();
            await action(scope.ServiceProvider.GetRequiredService<RestaurantDbContext>());
        }

        public async Task InitializeAsync()
        {
            // Arranca el servidor (migraciones y administrador de la plataforma).
            _ = Server;
            await Task.CompletedTask;
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            try
            {
                await WithDbAsync(db => db.Database.EnsureDeletedAsync());
            }
            finally
            {
                await base.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Con pocos intentos de login y detrás del proxy, como en producción con docker-compose.
    /// </summary>
    public class StrictLoginApiFactory : ApiFactory
    {
        public const int Limit = 3;
        protected override int LoginAttemptsPerMinute => Limit;
        protected override bool BehindProxy => true;
    }
}

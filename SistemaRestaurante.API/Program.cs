using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SistemaRestaurante.API.Auth;
using SistemaRestaurante.API.Controllers;
using SistemaRestaurante.API.Middlewares;
using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.DataAccess.Repositories;
using SistemaRestaurante.DataAccess.Seeders;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using SistemaRestaurante.Domain.Interfaces.Services;
using SistemaRestaurante.Domain.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers + JSON (los enums viajan como texto: "Pending", "MainCourse", ...)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sistema Restaurante API",
        Version = "v1",
        Description = "API para la gestión de reservas, mesas, clientes y menú de restaurantes"
    });

    // Botón "Authorize" en Swagger: pegar el token devuelto por POST /api/Auth/login.
    var bearer = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", bearer);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearer] = Array.Empty<string>() });
});

// Base de datos. La cadena de conexión se lee de la configuración
// (appsettings.Development.json en local, variable ConnectionStrings__DefaultConnection en producción).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Falta la cadena de conexión 'DefaultConnection'. Configúrala en appsettings.Development.json " +
        "o en la variable de entorno ConnectionStrings__DefaultConnection.");

// Restaurante de cada petición, sacado del token. La base filtra todo por él.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();

builder.Services.AddDbContext<RestaurantDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddAutoMapper(typeof(Program));

// Repositories
builder.Services.AddScoped<IRestaurantRepository, RestaurantRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IUserService, UserService>();

// ---------- Autenticación (JWT) y autorización por roles ----------
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Key) || Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
    throw new InvalidOperationException(
        "Falta la clave de firma 'Jwt:Key' o tiene menos de 32 caracteres. Configúrala en " +
        "appsettings.Development.json o en la variable de entorno Jwt__Key.");

builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // El token deja de valer si el usuario se desactiva, cambia su contraseña o su rol,
        // o si su restaurante se suspende.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal!;
                var stamp = principal.FindFirst(TokenService.StampClaim)?.Value;
                int userId;
                try { userId = principal.GetUserId(); }
                catch (UnauthorizedAccessException) { context.Fail("Token sin usuario"); return; }

                var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var user = await users.GetForAuthAsync(userId);
                var restaurantOk = user?.Role == UserRole.PlatformAdmin || user?.Restaurant is { IsActive: true };
                if (user == null || !user.IsActive || user.SecurityStamp != stamp || !restaurantOk)
                    context.Fail("La sesión ya no es válida");
            }
        };
    });

// Todo endpoint exige sesión salvo los marcados con [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Límite de intentos de login por IP, contra ataques de fuerza bruta (Auth:LoginAttemptsPerMinute, 10 por defecto).
var loginAttemptsPerMinute = builder.Configuration.GetValue("Auth:LoginAttemptsPerMinute", 10);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AuthController.LoginRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginAttemptsPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// CORS: solo los orígenes configurados en "Cors:AllowedOrigins".
const string FrontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Comprobación de salud para Docker y el hosting: la API responde y la base de datos está accesible.
builder.Services.AddHealthChecks().AddDbContextCheck<RestaurantDbContext>("database");

// Detrás de un proxy inverso (Caddy en docker-compose) la IP del cliente y el https llegan en
// cabeceras X-Forwarded-*. Solo se activa con ReverseProxy:Enabled y la API no debe quedar
// expuesta directamente: se confía en el último salto, que es el proxy.
var behindProxy = builder.Configuration.GetValue<bool>("ReverseProxy:Enabled");
if (behindProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var app = builder.Build();

if (behindProxy)
    app.UseForwardedHeaders();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Con proxy, el https lo pone el proxy.
if (!behindProxy)
    app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/health").AllowAnonymous();

// Migraciones y datos de ejemplo: solo si la configuración lo pide (activado en Development).
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();

    if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        await dbContext.Database.MigrateAsync();

    if (app.Configuration.GetValue<bool>("Database:SeedDemoData"))
        await DataSeeder.SeedAsync(dbContext);

    // Administrador de la plataforma (quien da de alta los restaurantes): solo si aún no existe.
    var initialAdmin = app.Configuration.GetSection("Auth:PlatformAdmin");
    var adminUser = initialAdmin["Username"];
    var adminPassword = initialAdmin["Password"];
    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    if (!string.IsNullOrWhiteSpace(adminUser) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        try
        {
            if (await userService.EnsurePlatformAdminAsync(adminUser, initialAdmin["FullName"] ?? "Administración", adminPassword))
                startupLogger.LogWarning("Creado el administrador de la plataforma {Username}. Cambia su contraseña tras el primer acceso.", adminUser);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            startupLogger.LogError("No se pudo crear el administrador de la plataforma: {Message}", ex.Message);
        }
    }
    else if (!await dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Role == UserRole.PlatformAdmin))
    {
        startupLogger.LogWarning("No hay administrador de la plataforma. Configura Auth:PlatformAdmin:Username y Auth:PlatformAdmin:Password.");
    }

    if (app.Configuration.GetValue<bool>("Database:SeedDemoData"))
        await DataSeeder.SeedDemoUsersAsync(dbContext, userService);
}

app.Run();

// Permite usar WebApplicationFactory<Program> en pruebas de integración.
public partial class Program { }

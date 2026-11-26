using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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

        // El token deja de valer si el usuario se desactiva o cambia su contraseña o su rol.
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
                var user = await users.GetByIdAsync(userId);
                if (user == null || user.SecurityStamp != stamp)
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

// Límite de intentos de login por IP, contra ataques de fuerza bruta.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AuthController.LoginRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
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

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Migraciones y datos de ejemplo: solo si la configuración lo pide (activado en Development).
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();

    if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        await dbContext.Database.MigrateAsync();

    if (app.Configuration.GetValue<bool>("Database:SeedDemoData"))
        await DataSeeder.SeedAsync(dbContext);

    // Primer administrador: solo se crea si la base no tiene ningún usuario.
    var initialAdmin = app.Configuration.GetSection("Auth:InitialAdmin");
    var adminUser = initialAdmin["Username"];
    var adminPassword = initialAdmin["Password"];
    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    if (!string.IsNullOrWhiteSpace(adminUser) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        if (await userService.EnsureInitialAdminAsync(adminUser, initialAdmin["FullName"] ?? "Administrador", adminPassword))
            startupLogger.LogWarning("Creado el administrador inicial {Username}. Cambia su contraseña tras el primer acceso.", adminUser);
    }
    else if (!await dbContext.Users.AnyAsync())
    {
        startupLogger.LogWarning("No hay usuarios. Configura Auth:InitialAdmin:Username y Auth:InitialAdmin:Password para crear el primero.");
    }

    if (app.Configuration.GetValue<bool>("Database:SeedDemoData"))
        await DataSeeder.SeedDemoUsersAsync(dbContext, userService);
}

app.Run();

// Permite usar WebApplicationFactory<Program> en pruebas de integración.
public partial class Program { }

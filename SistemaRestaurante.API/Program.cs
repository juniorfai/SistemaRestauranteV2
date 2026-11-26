using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SistemaRestaurante.API.Middlewares;
using SistemaRestaurante.DataAccess.Context;
using SistemaRestaurante.DataAccess.Repositories;
using SistemaRestaurante.DataAccess.Seeders;
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

// Services
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();
builder.Services.AddScoped<IReservationService, ReservationService>();

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
}

app.Run();

// Permite usar WebApplicationFactory<Program> en pruebas de integración.
public partial class Program { }

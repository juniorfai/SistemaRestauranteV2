# Restaurante Master: sistema de reservas para restaurantes

Aplicación web para gestionar las **reservas, mesas, clientes y la carta** de un restaurante.

- **Backend:** API REST en ASP.NET Core 8 con Entity Framework Core y SQL Server.
- **Frontend:** SPA en Angular 21 (componentes standalone y signals) con Bootstrap 5.

**Autores:** Ismael Barrios Domínguez, Tomás Muñoz Orosco.

## Funcionalidades

- **Reservas:** crear, editar, confirmar, cancelar, completar y marcar como "no asistió". Al elegir fecha, hora y número de personas se muestran solo las mesas libres. Incluye un pedido anticipado opcional con el total calculado.
- **Control de disponibilidad:** una reserva ocupa la mesa 2 horas (`ReservationRules.Duration`). La API impide reservas dobles, incluso con peticiones simultáneas, usando una transacción serializable.
- **Mesas:** alta, edición, cambio de estado (disponible, ocupada, reservada, fuera de servicio) y baja.
- **Clientes:** alta, edición, búsqueda y baja. El email es único.
- **Carta:** platos por categoría, disponibilidad, alta, edición y baja.
- **Borrado lógico:** nada se borra de verdad (`IsActive = false`), así se conserva el historial.

## Estructura

```
SistemaRestaurante.slnx
├── SistemaRestaurante.API/          Controladores, DTOs, AutoMapper, middleware de errores
├── SistemaRestaurante.Domain/       Entidades, reglas de negocio (servicios) e interfaces
├── SistemaRestaurante.DataAccess/   DbContext, repositorios, migraciones y datos de ejemplo
└── SistemaRestaurante-Front/        Aplicación Angular
```

## Requisitos

- .NET SDK 8 o superior
- SQL Server LocalDB (viene con Visual Studio) u otra instancia de SQL Server
- Node.js 20 LTS o superior (para el frontend)

## Puesta en marcha (desarrollo)

### 1. Backend

```powershell
cd SistemaRestaurante.API
dotnet run --launch-profile https
```

- La API queda en `https://localhost:7191` y Swagger en `https://localhost:7191/swagger`.
- En el entorno **Development** las migraciones se aplican solas y se cargan datos de ejemplo: un restaurante, 15 mesas, la carta, 4 clientes y algunas reservas.
- La cadena de conexión de desarrollo está en `appsettings.Development.json` y apunta a LocalDB (`RestaurantReservationDB`).

### 2. Frontend

```powershell
cd SistemaRestaurante-Front
npm install
npm start
```

Abre `http://localhost:4200`. La URL de la API se configura en `src/environments/environment.development.ts`.

## Configuración

| Clave | Para qué sirve | Desarrollo | Producción |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | Conexión a SQL Server | LocalDB | Variable de entorno `ConnectionStrings__DefaultConnection` |
| `Cors:AllowedOrigins` | Orígenes que pueden llamar a la API | `http://localhost:4200` | Dominio del frontend |
| `Database:ApplyMigrationsOnStartup` | Aplicar migraciones al arrancar | `true` | `false` (aplicarlas en el despliegue) |
| `Database:SeedDemoData` | Cargar datos de ejemplo si la base está vacía | `true` | `false` |

Las fechas de las reservas se guardan en la **hora local del restaurante**.

## API

Los errores siguen el formato **ProblemDetails**: `404` si el recurso no existe, `409` si una regla de negocio lo impide (choque de horario, email repetido, etc.) y `400` si los datos no son válidos. Los enums viajan como texto (`"Pending"`, `"MainCourse"`, ...).

| Recurso | Endpoints principales |
|---|---|
| Reservas | `GET /api/Reservation` (próximas), `GET /api/Reservation/date/{yyyy-MM-dd}`, `GET /api/Reservation/customer/{id}`, `POST`, `PUT /{id}`, `PATCH /{id}/confirm`, `/cancel`, `/complete`, `/no-show`, `DELETE /{id}` |
| Mesas | `GET /api/Table`, `GET /api/Table/available?dateTime=...&numberOfPeople=...`, `POST`, `PUT /{id}`, `PATCH /{id}/status`, `DELETE /{id}` |
| Clientes | `GET /api/Customer`, `GET /api/Customer/{id}`, `GET /api/Customer/email/{email}`, `POST`, `PUT /{id}`, `DELETE /{id}` |
| Carta | `GET /api/MenuItem?includeUnavailable=true`, `GET /api/MenuItem/category/{categoria}`, `POST`, `PUT /{id}`, `DELETE /{id}` |
| Restaurantes | `GET /api/Restaurant`, `GET /api/Restaurant/{id}`, `POST`, `PUT /{id}` |

Hay ejemplos listos para ejecutar en `SistemaRestaurante.API/SistemaRestaurante.API.http`.

## Pendiente (siguientes fases)

- Autenticación y roles (administrador, recepción).
- Soporte para varios restaurantes con datos separados.
- Pruebas automáticas del backend e integración continua.
- Despliegue (Docker y hosting).

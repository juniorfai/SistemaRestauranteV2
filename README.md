# Restaurante Master: sistema de reservas para restaurantes

Aplicación web para gestionar las **reservas, mesas, clientes y la carta** de un restaurante.

- **Backend:** API REST en ASP.NET Core 8 con Entity Framework Core y SQL Server.
- **Frontend:** SPA en Angular 21 (componentes standalone y signals) con Bootstrap 5.

**Autores:** Ismael Barrios Domínguez, Tomás Muñoz Orosco.

## Funcionalidades

- **Reservas:** crear, editar, confirmar, cancelar, completar y marcar como "no asistió". Al elegir fecha, hora y número de personas se muestran solo las mesas libres. Incluye un pedido anticipado opcional con el total calculado.
- **Control de disponibilidad:** una reserva ocupa la mesa 2 horas (`ReservationRules.Duration`). La API impide reservas dobles, incluso con peticiones simultáneas, usando una transacción serializable.
- **Mesas:** alta, edición, cambio de estado (disponible, ocupada, reservada, fuera de servicio) y baja.
- **Clientes:** alta, edición, búsqueda y baja. El email es único dentro de cada restaurante.
- **Carta:** platos por categoría, disponibilidad, alta, edición y baja.
- **Varios restaurantes:** un mismo programa da servicio a varios restaurantes con los datos separados. Cada usuario pertenece a un restaurante y solo ve sus mesas, carta, clientes, reservas y personal; la base de datos aplica el filtro en todas las consultas, y pedir un dato de otro restaurante devuelve `404`.
- **Administración de la plataforma:** un usuario especial (rol `PlatformAdmin`, sin restaurante) da de alta restaurantes junto con su primer administrador, edita sus datos y los suspende o reactiva. Un restaurante suspendido no puede entrar y sus sesiones abiertas se cierran; sus datos se conservan. Puede ver todo el personal y ponerle una contraseña nueva (soporte), pero no ve ni toca reservas, clientes, mesas ni carta.
- **Acceso con usuario y roles:** cada persona entra con su usuario. En cada restaurante hay tres roles:
  - **Administrador:** todo, incluida la gestión del personal (pantalla «Personal») y los datos del restaurante (pantalla «Mi restaurante»).
  - **Encargado:** reservas, clientes, mesas y carta.
  - **Camarero / sala:** reservas, alta y edición de clientes, y cambio de estado de las mesas. No borra clientes ni cambia la carta ni las mesas.
- **Borrado lógico:** nada se borra de verdad (`IsActive = false`), así se conserva el historial.

## Estructura

```
SistemaRestaurante.slnx
├── SistemaRestaurante.API/          Controladores, DTOs, AutoMapper, middleware de errores
├── SistemaRestaurante.Domain/       Entidades, reglas de negocio (servicios) e interfaces
├── SistemaRestaurante.DataAccess/   DbContext, repositorios, migraciones y datos de ejemplo
├── SistemaRestaurante.Tests/        Pruebas de integración de la API (xUnit + SQL Server)
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
- Usuarios de prueba en desarrollo:

  | Usuario | Contraseña | Rol |
  |---|---|---|
  | `plataforma` | `Plataforma123!` | Administración de la plataforma (alta de restaurantes) |
  | `admin` | `Admin123!` | Administrador |
  | `encargado` | `Encargado123!` | Encargado |
  | `camarero` | `Camarero123!` | Camarero / sala |

- Para crear migraciones usa la herramienta local del proyecto (versión 8): `dotnet tool restore` y después `dotnet tool run dotnet-ef migrations add Nombre --project SistemaRestaurante.DataAccess --startup-project SistemaRestaurante.API`.

### 2. Frontend

```powershell
cd SistemaRestaurante-Front
npm install
npm start
```

Abre `http://localhost:4200`. La URL de la API se configura en `src/environments/environment.development.ts`.

## Pruebas

### Backend

```bash
dotnet test SistemaRestaurante.Tests
```

Son pruebas de integración: cada clase levanta la API completa en memoria contra una base de SQL Server **propia y vacía** (`SistemaRestaurante_Test_<guid>`), creada con las migraciones reales y borrada al terminar. Nunca tocan la base de desarrollo. Cubren:

- **Separación entre restaurantes:** listados, lectura y modificación de datos ajenos (`404`), reservas con mesas, clientes o platos de otro restaurante, personal ajeno.
- **Permisos por rol** (administrador, encargado, camarero) y del administrador de la plataforma, incluida la suspensión de restaurantes.
- **Sesión:** token falso, cambio de contraseña y usuario desactivado cierran sesiones, límite de intentos de login.
- **Reservas:** choque de horario (2 h), peticiones simultáneas a la misma mesa (solo una se guarda), capacidad, mesas fuera de servicio, fechas pasadas, cambios de estado, total del pedido anticipado.

Por defecto usan LocalDB. Para otro servidor, define `TEST_SQLSERVER` con la cadena de conexión **sin** base de datos, por ejemplo `Server=localhost,1433;User Id=sa;Password=...;TrustServerCertificate=True`.

### Frontend

```bash
cd SistemaRestaurante-Front
npx ng test --watch=false
```

### Integración continua

GitHub Actions (`.github/workflows/ci.yml`) se ejecuta en cada pull request y en cada push a `main`:

- **Backend:** compila la solución, comprueba que no falte ninguna migración (`has-pending-model-changes`) y pasa las pruebas contra un SQL Server 2022 en contenedor.
- **Frontend:** `npm ci`, compilación de producción y pruebas.

Conviene activar en GitHub la protección de `main` (Settings → Branches) exigiendo que los dos trabajos pasen antes de fusionar.

## Configuración

| Clave | Para qué sirve | Desarrollo | Producción |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | Conexión a SQL Server | LocalDB | Variable de entorno `ConnectionStrings__DefaultConnection` |
| `Cors:AllowedOrigins` | Orígenes que pueden llamar a la API | `http://localhost:4200` | Dominio del frontend |
| `Database:ApplyMigrationsOnStartup` | Aplicar migraciones al arrancar | `true` | `false` (aplicarlas en el despliegue) |
| `Database:SeedDemoData` | Cargar datos de ejemplo y el personal de prueba del primer restaurante si la base está vacía | `true` | `false` |
| `Jwt:Key` | Clave secreta para firmar las sesiones (mínimo 32 caracteres) | Clave de desarrollo | Variable `Jwt__Key`, larga y aleatoria. **Obligatoria** |
| `Jwt:ExpiresMinutes` | Duración de la sesión | 720 (12 h) | 720 |
| `Auth:LoginAttemptsPerMinute` | Intentos de login por minuto y por IP | 10 | 10 |
| `Auth:PlatformAdmin:Username` / `FullName` / `Password` | Administrador de la plataforma, se crea al arrancar si todavía no existe ninguno | `plataforma` / `Plataforma123!` | Variables `Auth__PlatformAdmin__Username` y `Auth__PlatformAdmin__Password`; cambia la contraseña tras el primer acceso y después puedes quitar las variables |

Las fechas de las reservas se guardan en la **hora local del restaurante**.

### Dar de alta un restaurante

1. Entra con el usuario de la plataforma: la aplicación abre la pantalla «Restaurantes».
2. Pulsa **Nuevo restaurante**, rellena sus datos y los del primer administrador (usuario y contraseña inicial).
3. Pasa esos datos al cliente. Su administrador entra, cambia la contraseña en «Mi cuenta» y da de alta al resto del personal, las mesas y la carta.

Al actualizar una base anterior a esta versión, la migración `MultiRestaurant` asigna los clientes, las reservas y el personal que ya existían al primer restaurante (las reservas, al restaurante de su mesa).

## API

Todos los endpoints exigen sesión salvo `POST /api/Auth/login`, que devuelve un token para la cabecera `Authorization: Bearer <token>` (en Swagger, botón **Authorize**). El token lleva el restaurante del usuario: no hace falta (ni se puede) indicar `restaurantId` en las peticiones. El login admite 10 intentos por minuto y por IP. Cambiar la contraseña o el rol, desactivar a un usuario o suspender su restaurante cierra sus sesiones abiertas.

Los errores siguen el formato **ProblemDetails**: `401` sin sesión válida, `403` si el rol no tiene permiso, `404` si el recurso no existe, `409` si una regla de negocio lo impide (choque de horario, email repetido, etc.) y `400` si los datos no son válidos. Los enums viajan como texto (`"Pending"`, `"MainCourse"`, ...).

| Recurso | Endpoints principales |
|---|---|
| Sesión | `POST /api/Auth/login`, `GET /api/Auth/me`, `POST /api/Auth/change-password` |
| Personal | `GET /api/User`, `POST`, `PUT /{id}` (nombre, rol, activo), `POST /{id}/reset-password`. Administrador del restaurante; la plataforma solo puede listar y resetear contraseñas |
| Reservas | `GET /api/Reservation` (próximas), `GET /api/Reservation/date/{yyyy-MM-dd}`, `GET /api/Reservation/customer/{id}`, `POST`, `PUT /{id}`, `PATCH /{id}/confirm`, `/cancel`, `/complete`, `/no-show`, `DELETE /{id}` |
| Mesas | `GET /api/Table`, `GET /api/Table/available?dateTime=...&numberOfPeople=...`, `POST`, `PUT /{id}`, `PATCH /{id}/status`, `DELETE /{id}` |
| Clientes | `GET /api/Customer`, `GET /api/Customer/{id}`, `GET /api/Customer/email/{email}`, `POST`, `PUT /{id}`, `DELETE /{id}` |
| Carta | `GET /api/MenuItem?includeUnavailable=true`, `GET /api/MenuItem/category/{categoria}`, `POST`, `PUT /{id}`, `DELETE /{id}` |
| Restaurantes | `GET /api/Restaurant/current` (el del usuario). Plataforma: `GET /api/Restaurant`, `POST` (con `adminUsername`, `adminFullName`, `adminPassword`), `PATCH /{id}/active`. `GET /{id}` y `PUT /{id}`: plataforma o administrador de ese restaurante |

Hay ejemplos listos para ejecutar en `SistemaRestaurante.API/SistemaRestaurante.API.http`.

## Pendiente (siguientes fases)

- Despliegue (Docker y hosting).

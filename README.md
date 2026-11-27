# Restaurante Master: sistema de reservas para restaurantes

Aplicación web para gestionar las **reservas, mesas, clientes y la carta** de un restaurante, y el **servicio en mesa**: los camareros toman las comandas y la cocina y la barra las ven en su pantalla.

- **Backend:** API REST en ASP.NET Core 8 con Entity Framework Core y SQL Server.
- **Frontend:** SPA en Angular 21 (componentes standalone y signals) con Bootstrap 5.

**Autores:** Ismael Barrios Domínguez, Tomás Muñoz Orosco.

## Funcionalidades

- **Reservas:** crear, editar, confirmar, cancelar, completar y marcar como "no asistió". Al elegir fecha, hora y número de personas se muestran solo las mesas libres. Incluye un pedido anticipado opcional con el total calculado.
- **Control de disponibilidad:** una reserva ocupa la mesa 2 horas (`ReservationRules.Duration`). La API impide reservas dobles, incluso con peticiones simultáneas, usando una transacción serializable.
- **Mesas:** alta, edición, cambio de estado (disponible, ocupada, reservada, fuera de servicio) y baja.
- **Clientes:** alta, edición, búsqueda y baja. El email es único dentro de cada restaurante.
- **Carta:** platos por categoría, disponibilidad, alérgenos, zona que lo prepara, alta, edición y baja.
- **Servicio en mesa (comandas):** el camarero abre la cuenta de la mesa (o sienta una reserva, que trae su pedido anticipado), apunta platos con notas («sin cebolla») y tiempos (entrante, principal, postre) y los envía a cocina; puede retener los segundos y «marcharlos» después. Cada plato va a su **zona de preparación** (por defecto Cocina, y Barra para las bebidas; se pueden crear más, con la dirección de su impresora de comandas) y cada zona ve sus comandas pendientes, las pasa a «en preparación» y «listo». Al final se saca la **cuenta provisional** con la propina sugerida (voluntaria, 10 % por defecto) y se cierra indicando forma de pago y propina, lo que libera la mesa. No emite facturas: el cobro y la factura electrónica (DIAN) van por el sistema propio del restaurante.
  - **Tiempo real:** la pantalla de cocina (`/cocina`) recibe las comandas al momento, de la más antigua a la más nueva, con el tiempo de espera en verde, ámbar (10 min) y rojo (20 min) y las notas resaltadas. Se toca un plato para pasarlo a «preparando» y otra vez a «listo», o «Todo listo» para despachar la comanda (y «Recuperar» si fue por error). Filtro por zona, aviso sonoro opcional y pantalla completa. El plano de sala y la cuenta se actualizan solos, y el camarero recibe un aviso cuando sus platos están listos. Si se corta la conexión, reintenta sola y mientras tanto las pantallas se recargan cada pocos segundos.
  - **Cobro:** desde la cuenta de la mesa, «Cuenta y cobro» muestra la cuenta provisional lista para imprimir en tiquetera de 80 mm (con la propina sugerida, el aviso de que es voluntaria y «no es factura de venta»). El cliente elige propina (sugerida, sin propina u otra cantidad) y forma de pago; en efectivo se calculan las vueltas. No deja cobrar con platos aún en cocina, pide confirmación y, al cerrar, libera la mesa y permite reimprimir el comprobante.
  - **Caja del día** (encargado y administrador): ventas, propinas y total cobrado, desglose por forma de pago y lista de cuentas cerradas y anuladas de cualquier día, con acceso a cada comprobante.
  - Una mesa solo tiene una cuenta abierta (lo garantiza la base de datos, aunque dos camareros la abran a la vez); reenviar un pedido por un corte de Wi-Fi no duplica platos; si dos pantallas cambian el mismo plato a la vez, la segunda recibe `409` y se actualiza.
  - Nombre y precio se copian al pedir: cambiar la carta no altera cuentas abiertas ni cerradas. Lo enviado no se borra: lo anula el encargado indicando el motivo.
  - Moneda (COP por defecto) y propina sugerida se configuran por restaurante («Mi restaurante»); los importes se muestran con formato colombiano ($ 25.000).
  - **Pantalla «Sala»** (inicio del camarero): plano de mesas con quién atiende cada una, tiempo, importe y avisos de platos listos, sin enviar o retenidos; se refresca sola cada 15 s. Al tocar una mesa libre se sientan los clientes (con reserva opcional); al tocar una ocupada se abre su cuenta.
  - **Pantalla de la cuenta:** carta con buscador y alérgenos a un lado, comanda al otro. Lo que se apunta queda guardado en la tableta hasta enviarlo (sobrevive a una recarga) y cada plato lleva un identificador para que un reintento no lo duplique. Notas y tiempo por plato, retener los platos fuertes y postres y marcharlos después, servir (o deshacer), cambiar comensales o de mesa; el encargado anula platos o la cuenta.
  - **Zonas** (encargado): alta de zonas con su impresora. En la carta, cada plato indica su zona y sus alérgenos.
- **Varios restaurantes:** un mismo programa da servicio a varios restaurantes con los datos separados. Cada usuario pertenece a un restaurante y solo ve sus mesas, carta, clientes, reservas y personal; la base de datos aplica el filtro en todas las consultas, y pedir un dato de otro restaurante devuelve `404`.
- **Administración de la plataforma:** un usuario especial (rol `PlatformAdmin`, sin restaurante) da de alta restaurantes junto con su primer administrador, edita sus datos y los suspende o reactiva. Un restaurante suspendido no puede entrar y sus sesiones abiertas se cierran; sus datos se conservan. Puede ver todo el personal y ponerle una contraseña nueva (soporte), pero no ve ni toca reservas, clientes, mesas ni carta.
- **Acceso con usuario y roles:** cada persona entra con su usuario. En cada restaurante hay tres roles:
  - **Administrador:** todo, incluida la gestión del personal (pantalla «Personal») y los datos del restaurante (pantalla «Mi restaurante»).
  - **Encargado:** reservas, clientes, mesas y carta.
  - **Camarero / sala:** reservas, alta y edición de clientes, cambio de estado de las mesas y comandas (abrir, apuntar, enviar, servir, cobrar). No borra clientes, no cambia la carta ni las mesas y no anula lo enviado a cocina.
  - **Cocina:** solo la pantalla de cocina y barra (comandas pendientes y su estado).
  - Encargado y administrador, además, anulan platos enviados o cuentas enteras (con motivo) y ven el histórico de cuentas por día.
- **Borrado lógico:** nada se borra de verdad (`IsActive = false`), así se conserva el historial.

## Estructura

```
SistemaRestaurante.slnx
├── SistemaRestaurante.API/          Controladores, DTOs, AutoMapper, middleware de errores
├── SistemaRestaurante.Domain/       Entidades, reglas de negocio (servicios) e interfaces
├── SistemaRestaurante.DataAccess/   DbContext, repositorios, migraciones y datos de ejemplo
├── SistemaRestaurante.Tests/        Pruebas de integración de la API (xUnit + SQL Server)
├── SistemaRestaurante-Front/        Aplicación Angular (+ Dockerfile y Caddyfile del servidor web)
├── docker-compose.yml               Producción: SQL Server + API + web con HTTPS
└── scripts/backup.sh                Copia de seguridad de la base de datos
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
  | `cocina` | `Cocina123!` | Pantalla de cocina |

- Para crear migraciones usa la herramienta local del proyecto (versión 8): `dotnet tool restore` y después `dotnet tool run dotnet-ef migrations add Nombre --project SistemaRestaurante.DataAccess --startup-project SistemaRestaurante.API`.

### 2. Frontend

```powershell
cd SistemaRestaurante-Front
npm install
npm start
```

Abre `http://localhost:4200`. La URL de la API se configura en `src/environments/environment.development.ts`.

## Despliegue en producción (Docker)

Todo funciona con **Docker Compose** en un único servidor:

| Servicio | Qué es | Expuesto |
|---|---|---|
| `web` | [Caddy](https://caddyserver.com) con la aplicación Angular; reenvía `/api/*` a la API y obtiene y renueva solo el certificado HTTPS | Puertos 80 y 443 |
| `api` | La API .NET 8 (usuario sin privilegios, comprobación de salud en `/api/health`) | No, solo red interna |
| `db` | SQL Server 2022 **Express** (gratuita en producción, hasta 10 GB por base) | No, solo red interna |

### Servidor

- Linux con Docker (por ejemplo, un VPS de Hetzner, DigitalOcean, OVH o Azure/AWS). Mínimo **2 GB de RAM** (SQL Server los necesita) y 2 vCPU; 4 GB si vas a tener muchos restaurantes.
- Un dominio o subdominio (p. ej. `reservas.midominio.com`) con un registro DNS **A** apuntando a la IP del servidor.
- Puertos **80** y **443** abiertos en el cortafuegos.

### Primera instalación

```bash
git clone https://github.com/juniorfai/SistemaRestauranteV2.git restaurante
cd restaurante
cp .env.example .env
nano .env        # SITE_ADDRESS=tu dominio, DB_PASSWORD, JWT_KEY, PLATFORM_ADMIN_PASSWORD
docker compose up -d --build
docker compose ps   # los tres servicios deben aparecer como "healthy" / "running"
```

Al arrancar, la API aplica las migraciones y crea el administrador de la plataforma. Entra en `https://tu-dominio` con ese usuario, **cambia su contraseña** en «Mi cuenta» y da de alta el primer restaurante.

Para probarlo en tu ordenador antes de contratar nada: deja `SITE_ADDRESS=:80` (y `HTTP_PORT=8080` si el 80 está ocupado) y abre `http://localhost` (o `http://localhost:8080`).

### Actualizar a una versión nueva

```bash
git pull
docker compose up -d --build
```

Las migraciones nuevas se aplican solas al arrancar la API. Haz una copia de seguridad antes.

### Copias de seguridad

```bash
./scripts/backup.sh     # deja un .bak en backups/ y borra los de más de 14 días
```

Prográmalo a diario con `crontab -e` → `0 4 * * * cd /ruta/restaurante && ./scripts/backup.sh` y copia la carpeta `backups/` fuera del servidor. Para restaurar una copia:

```bash
docker compose stop api
docker compose cp backups/RestauranteMaster-AAAAMMDD-HHMMSS.bak db:/var/opt/mssql/restore.bak
docker compose exec db sh -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "RESTORE DATABASE [RestauranteMaster] FROM DISK = N'"'"'/var/opt/mssql/restore.bak'"'"' WITH REPLACE"'
docker compose exec db rm /var/opt/mssql/restore.bak
docker compose start api
```

### Mantenimiento

- Registros: `docker compose logs -f api` (o `web`, `db`).
- Estado: `curl https://tu-dominio/api/health` responde `Healthy` si la API y la base funcionan.
- Los datos viven en volúmenes de Docker (`db-data`, y los certificados en `caddy-data`): `docker compose down` no los borra; `docker compose down -v` **sí**.

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
- **Comandas:** flujo completo de la mesa al cobro, zonas (Cocina/Barra por defecto, borrado protegido), una cuenta abierta por mesa con aperturas simultáneas, reintentos sin duplicados, platos retenidos y marchados, cambios de estado desde dos pantallas, anulaciones con motivo, cierre con platos pendientes, propina redondeada a pesos, sentar una reserva, permisos del rol Cocina y separación entre restaurantes.
- **Tiempo real:** los avisos solo llegan a las pantallas del mismo restaurante, el camarero recibe qué platos están listos, conexión por WebSocket con el token en la URL (como el navegador) y el hub exige sesión de un restaurante.

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
- **Docker:** construye las imágenes, levanta `docker-compose.yml` completo y comprueba que la web y la API responden.

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
| `ReverseProxy:Enabled` | La API está detrás de un proxy (toma la IP real de `X-Forwarded-For` y deja el HTTPS al proxy). No exponer la API directamente con esto activado | `false` | `true` en docker-compose |
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
| Zonas | `GET /api/Station`, `POST`, `PUT /{id}`, `DELETE /{id}` (escritura: encargado o administrador) |
| Comandas | `GET /api/Order/open`, `GET /api/Order/{id}`, `POST /api/Order` (`tableId`, `guests`, `reservationId?`), `POST /{id}/items` (`items[]` con `menuItemId`, `quantity`, `notes`, `course`, `clientRequestId`), `PUT`/`DELETE /{id}/items/{itemId}` (solo borradores), `POST /{id}/send` (`holdFromCourse?`), `POST /{id}/fire` (`course`), `PUT /{id}/items/{itemId}/status`, `PUT /{id}/table`, `PUT /{id}/guests`, `GET /{id}/bill`, `POST /{id}/close` (`paymentMethod`, `tipAmount`). Encargado: `POST /{id}/items/{itemId}/cancel`, `POST /{id}/cancel` (`reason`), `GET /api/Order/history?date=yyyy-MM-dd` |
| Cocina | `GET /api/Kitchen/tickets?stationId=`, `PUT /api/Kitchen/orders/{id}/items/{itemId}/status` (`status`, `expectedStatus`), `POST /api/Kitchen/orders/{id}/stations/{stationId}/bump` (todo listo) |
| Tiempo real | Hub SignalR en `/api/hubs/orders` (roles del restaurante; el token va en `?access_token=`, que solo se acepta en esa ruta y Caddy no guarda en el log). Eventos: `OrderChanged` (`orderId`) y `ItemsReady` (`orderId`, `tableNumber`, `waiterId`, `items`) |
| Restaurantes | `GET /api/Restaurant/current` (el del usuario). Plataforma: `GET /api/Restaurant`, `POST` (con `adminUsername`, `adminFullName`, `adminPassword`), `PATCH /{id}/active`. `GET /{id}` y `PUT /{id}`: plataforma o administrador de ese restaurante |

Hay ejemplos listos para ejecutar en `SistemaRestaurante.API/SistemaRestaurante.API.http`.


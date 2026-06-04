Ismael Barrios Dominguez 
Tomas Muñoz orosco 

BACKEND
Restaurante Master - Backend (API REST)

Este directorio contiene la capa del servidor y la persistencia de datos del sistema de gestión del restaurante, desarrollada bajo una arquitectura robusta y escalable.

Tecnologías Utilizadas
Lenguaje: C# (.NET 8.0)
Framework Web: ASP.NET Core Web API
ORM: Entity Framework Core (Enfoque Code-First)
Base de Datos: SQL Server
Documentación: Swagger UI / OpenAPI

Requisitos Previos (Prerrequisitos)

Antes de ejecutar el proyecto, asegúrese de tener instalado lo siguiente en su máquina local:

 Para el Backend y Base de Datos:
SDK de .NET 8.0 (o la versión correspondiente utilizada).
Visual Studio 2022 (con la carga de trabajo Desarrollo de ASP.NET y web activa).
SQL Server Express u otra instancia local de SQL Server.
SQL Server Management Studio (SSMS)** (Opcional, para verificar tablas).


PUERTOS Y CONEXIÓN
Para que la aplicación Front-end pueda comunicarse exitosamente con esta API, el número de puerto configurado aquí debe coincidir estrictamente con las llamadas HTTP del cliente.

Puerto HTTPS por defecto del proyecto:** `7191`
URL Base local:** `https://localhost:7191`

Cómo verificar el puerto asignado:
1. Abra la carpeta `Properties`en este directorio.
2. Inspeccione el archivo `launchSettings.json`.
3. En el perfil de configuración `https`, localice el atributo `applicationUrl` y asegúrese de que el puerto seguro sea el*7191:
   ```json
   "applicationUrl": "https://localhost:7191;http://localhost:5000"

Pasos para la Configuración y Despliegue Local
Abrir el proyecto: Inicie Visual Studio 2022 y cargue el archivo de solución (.slnx) de este directorio.

Configurar la Base de Datos: Abra el archivo appsettings.json y modifique la cadena de conexión para que apunte a su instancia local de SQL Server (Express o LocalDB):

JSON
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=RestauranteDB;Trusted_Connection=True;TrustServerCertificate=True;"
}

Generar el Esquema y Sembrado (Seeding): Vaya a Herramientas > Administrador de paquetes NuGet > Consola del Administrador de paquetes y ejecute el comando de migración:

PowerShell
Update-Database

Este comando creará automáticamente la base de datos RestauranteDB, estructurará las tablas (Mesas, Clientes, MenuItems) e inyectará de forma automática los datos iniciales de prueba.

Ejecutar el Servidor: Presione la tecla F5 o dé clic al botón Play. El navegador web se abrirá automáticamente desplegando la interfaz interactiva de Swagger en:

https://localhost:7191/swagger

FRONTEND

Restaurante Master - Frontend (Angular SPA)


REQUISITOS PREVIOS (PRERREQUISITOS)
Para el Frontend:
Node.js (Versión LTS recomendada).
Angular CLI instalado de forma global (`npm install -g @angular/cli`).
Visual Studio Code (Editor recomendado para el Frontend).

Este directorio contiene la interfaz gráfica de usuario del sistema del restaurante, diseñada como una Single Page Application (SPA) responsiva, dinámica e intuitiva.

Tecnologías Utilizadas
Framework: Angular (TypeScript)
Estilos y Maquetación: Bootstrap & Bootstrap Icons
Estrategia de Componentes: Componentes Standalone (Modernos)
Gestión de Rutas: Angular Router


COMUNICACIÓN CON LA API
Esta aplicación web consume datos externos de una API en .NET. Si las tarjetas, las listas o las mesas aparecen completamente en blanco o vacías, significa que hay un desfase en el puerto de conexión.

Puerto esperado de la API: `https://localhost:7191`

Cómo actualizar el puerto si el Backend cambió:
1. Si el servidor de C# corre en un puerto diferente (como el puerto `5000`), usted debe reconfigurar las URLs en Angular.
2. Vaya a la carpeta de servicios de la aplicación (ej. `src/app/services/menu.service.ts`, `customer.service.ts`, etc.).
3. Modifique la variable `apiUrl` para que apunte exactamente al puerto que arrojó el servidor del Backend:
   ```typescript

   // Asegúrese de que coincida con el puerto activo del Backend
   private apiUrl = 'https://localhost:7191/api/menuitems';

Pasos para la Configuración y Ejecución Local
Asegúrese de contar con Node.js y Angular CLI (npm install -g @angular/cli) instalados de forma global en su máquina antes de iniciar.

Abrir el proyecto: Inicie Visual Studio Code y abra directamente esta carpeta (SistemaRestaurante-Front).

Restaurar Dependencias: Despliegue una nueva terminal integrada en VS Code y descargue los paquetes de Node descritos en el proyecto ejecutando:

Bash
npm install
Espere a que finalice la descarga; esto creará la carpeta local indispensable node_modules.

Levantar el Servidor de Desarrollo: En la misma terminal de VS Code, ejecute el comando de compilación en tiempo real:

Bash
ng serve

Acceso a la Aplicación: Una vez que la terminal indique de forma exitosa Compiled successfully, abra su navegador de preferencia e ingrese a la siguiente dirección:


http://localhost:4200

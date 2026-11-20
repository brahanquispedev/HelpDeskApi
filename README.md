# HelpDesk API — proyecto del equipo

Sistema de soporte técnico desarrollado por un equipo de **seis integrantes**. Permite organizar solicitudes, asignarlas a equipos y técnicos, registrar su atención y consultar su historial y reportes.

El backend utiliza **.NET 10, PostgreSQL, Entity Framework Core, MediatR y autenticación JWT**. El diseño de datos contempla **36 tablas y 2 vistas**.

## Equipo y distribución actual

| Integrante | Entidades y responsabilidades |
|---|---|
| **Diego Medina** | Autenticación, Usuarios, Roles, UsuariosRoles, Permisos, RolesPermisos y RecuperacionesClave. Coordina los flujos de acceso y la inicialización del administrador. |
| **Gianfranco Paolo** | Cambios, CambiosTickets, TareasCambios, Aprobaciones, Problemas y ProblemasTickets. |
| **Fernando Aquima** | Activos, TiposActivos, AsignacionesActivos, TicketsActivos, Comentarios y Adjuntos. |
| **Stiven Yerai** | Sedes, Areas, EquiposSoporte, IntegrantesEquipo, Notificaciones, PreferenciasNotificaciones y EnviosNotificaciones. |
| **Ederson Vilca** | Servicios, CategoriasServicios, PoliticasAtencion, Encuestas, ArticulosConocimiento, CategoriasConocimiento y TicketsArticulos. |
| **Brahan Quispe** | Tickets, AsignacionesTickets, HistorialTickets, Auditoria, CargaTecnicos, ResumenTickets y Reportes. También coordina Domain compartido, Infrastructure, Common, reglas de acceso e integración de la API. |

Cada responsable incorpora los **DTOs, casos de uso y controladores** de sus entidades en commits identificables. Los modelos generados, repositorios, UnitOfWork y servicios técnicos son componentes compartidos que se coordinan con Brahan.

`Application/Services` contiene los servicios de JWT y hash, a cargo de Brahan, y `AuthService`, a cargo de Diego. El módulo de negocio `Servicios`, que representa el catálogo de atenciones, corresponde a Ederson.

La distribución exhaustiva de la primera versión está en la [guía de la plantilla](https://github.com/BrahanPRANK/HelpDeskApi/blob/main/HelpDeskBackend/DISTRIBUCION_EQUIPO.md). Esa guía distingue los archivos de la plantilla y los nombres de los proyectos del equipo; su inventario de avance corresponde a la fecha de revisión indicada allí.

## Organización de la solución

```text
HelpDeskApi/
├── HelpDeskApi.sln
├── HelpDeskApi/              Controladores, arranque y configuración de la API
├── HelpDesk.Application/     Commands, Queries, handlers y reglas de acceso
├── HelpDesk.Domain/          Modelos, DTOs, interfaces y excepciones
├── HelpDesk.Infrastructure/  DbContext, repositorios, UnitOfWork y servicios
└── scripts/                 Herramientas de apoyo, como scaffolding
```

El controlador recibe una petición y la envía a MediatR. El handler ejecuta el caso de uso, consulta o modifica entidades y construye el DTO de salida. Las escrituras se confirman mediante `IUnitOfWork.Complete()`.

Los DTOs se organizan en `HelpDesk.Domain/Dtos/{Entidad}`, uno por archivo. Los modelos y el contexto ya están incluidos. EF representa cuatro tablas de relación dentro del contexto, sin una clase independiente; por ello hay 34 clases generadas para las 36 tablas y las 2 vistas.

## Estado de la primera versión

`main` contiene la base compartida y los aportes integrados mediante Pull Requests. Ya incluye casos de uso, DTOs y controladores de los módulos incorporados por el equipo; las responsabilidades de la tabla no significan que todas las funciones estén terminadas o verificadas.

El flujo básico de Tickets es:

```text
nuevo → asignado → en_atencion → resuelto → cerrado
```

Asignaciones e historial se mantienen desde las acciones de Tickets. CargaTecnicos y ResumenTickets son recursos de consulta. Auditoria permite consultar los registros disponibles; el registro automático de auditoría queda para otra entrega.

La configuración integrada de `Program.cs` utiliza `AddApplicationServices`, `AddInfrastructureServices`, `AddApiServices` y `UseApiPipeline`. La API registra Swagger con JWT durante desarrollo, el middleware de excepciones y respuestas uniformes para los errores HTTP. El límite de login y registro es de 20 intentos por minuto y dirección IP. `/health` comprueba el arranque; no sustituye una comprobación de conexión a PostgreSQL.

Este repositorio publica el backend; el frontend React se mantiene por separado. Esta versión no incluye proveedor real de correo ni carga física de archivos. Los adjuntos administran metadatos. La recuperación de contraseña y los flujos avanzados se coordinan para entregas posteriores.

## Clonar y compilar

Requisitos: Git, .NET SDK 10.0 y PostgreSQL 16 o superior para utilizar la persistencia.

```powershell
git clone https://github.com/brahanquispedev/HelpDeskApi.git
cd HelpDeskApi
dotnet restore .\HelpDeskApi\HelpDeskApi.sln
dotnet build .\HelpDeskApi\HelpDeskApi.sln
```

## Preparar la base y la configuración local

Cada integrante utiliza su propia base PostgreSQL. Debe crear `helpdeskdb` y aplicar el script de 36 tablas acordado por el equipo antes de ejecutar operaciones de persistencia. La API no crea ese esquema automáticamente.

El SQL se comparte por el medio acordado con Brahan; actualmente no forma parte de este repositorio de trabajo. No utilizar el diseño histórico de 25 tablas.

Configurar en User Secrets del proyecto `HelpDeskApi/HelpDeskApi` o mediante variables de entorno:

- `ConnectionStrings:DefaultConnection`: conexión PostgreSQL de la computadora.
- `Jwt:Key`: clave local de firma, de al menos 32 bytes.
- `Jwt:Issuer`: emisor configurado para la API.
- `Jwt:Audience`: audiencia configurada para los clientes.

Los secretos de otra computadora no se transfieren al clonar. No escribir contraseñas, claves JWT ni tokens en archivos versionados.

Para iniciar con el perfil de desarrollo HTTP:

```powershell
dotnet run --project .\HelpDeskApi\HelpDeskApi --launch-profile http
```

El perfil utiliza `http://localhost:5086`. `GET /health` expone el estado básico de la aplicación; su configuración actual no comprueba por sí sola la conexión con PostgreSQL.

## Scaffolding

Los modelos ya están en el repositorio: no es necesario regenerarlos al clonar si la base utiliza el esquema acordado.

El script `HelpDeskApi/scripts/Actualizar-Modelo.ps1` lee PostgreSQL y regenera modelos y contexto. Se utiliza únicamente ante cambios coordinados del esquema. No crea tablas ni crea al administrador. Conservar `HelpDeskDbContext.Custom.cs`, que contiene las correcciones de relaciones y concurrencia.

## Trabajo por ramas y commits

`main` contiene lo compartido. Para desarrollar, actualizar primero la copia local y crear una rama propia:

```powershell
git switch main
git pull --ff-only origin main
git switch -c feature/nombre-avance
```

Si ya existe la rama personal, usar `git switch feature/nombre-avance` en vez de crearla otra vez. Guardar o confirmar el trabajo local antes de cambiar de rama.

Realizar commits por entidad o por un cambio compartido concreto. Incluir los DTOs que necesitan los handlers para compilar. Ejemplo de un avance de Cambios:

```powershell
git status
git add .\HelpDeskApi\HelpDesk.Application\UseCases\Cambios\
git add .\HelpDeskApi\HelpDesk.Domain\Dtos\Cambios\
git add .\HelpDeskApi\HelpDeskApi\Controllers\CambiosController.cs
git commit -m "Agregar casos de uso, DTO y controlador de Cambios"
git push -u origin feature/nombre-avance
```

Abrir un Pull Request con **base `main`** y la rama personal como origen. Revisar los archivos, resolver conflictos y comprobar la compilación antes de fusionar. Si ya existe un PR abierto de esa rama, los nuevos commits aparecen en el mismo PR.

Cada integrante utiliza su propia cuenta de GitHub y necesita acceso como colaborador para subir ramas al repositorio del equipo.

## Coordinación e integración

- Coordinar cambios del contexto, modelos, solución, paquetes y contratos compartidos con Brahan.
- Coordinar autenticación con Diego y su integración de arranque con Brahan.
- No reemplazar `Program.cs` desde cada módulo ni incorporar usuarios simulados con todos los permisos a la configuración compartida.
- Revisar las diferencias de ambas ramas al resolver conflictos; no aceptar todos los cambios sin revisar el archivo.
- Excluir `bin`, `obj`, `.idea`, credenciales y configuración privada de los commits.
- Comprobar manualmente los endpoints del módulo con datos y permisos apropiados después de compilar.

La revisión final debe comprobar el flujo integrado de login, creación de ticket, asignación, atención, resolución y cierre. La compilación por sí sola no verifica toda la lógica contra la base.


## Configuración final para el frontend

La API de desarrollo usa `http://localhost:5086`; el frontend Vite usa
`http://localhost:5173` y `VITE_API_BASE_URL=http://localhost:5086`.
Swagger está en `/` y su documento en `/swagger/v1/swagger.json`, durante desarrollo.

`Cors:AllowedOrigins` contiene los orígenes exactos autorizados. En desarrollo se
permite `http://localhost:5173`; para otro puerto u host hay que declararlo
explícitamente. En producción no se habilitan orígenes por defecto: configura
`Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, etc. No se utiliza `*` ni cookies
de autenticación. Cada petición protegida envía `Authorization: Bearer <token>`.

Configura `ConnectionStrings:DefaultConnection`, `Jwt:Key`, `Jwt:Issuer` y
`Jwt:Audience` mediante User Secrets en desarrollo o variables del entorno en
producción. La clave de firma debe tener al menos 32 bytes. Nunca la incluyas en
el frontend ni en archivos versionados. En producción utiliza HTTPS y el origen
real del frontend.

### Administrador inicial

Con la base de 36 tablas preparada y sus roles iniciales cargados, ejecuta desde
la raíz del repositorio:

```powershell
.\HelpDeskApi\scripts\Crear-Administrador.ps1 -NombreUsuario admin -Correo admin@example.com
```

Sustituye el correo de ejemplo. El script solicita la contraseña de forma oculta,
ejecuta el modo de consola `--crear-admin` y restaura las variables temporales al
terminar. No publica un endpoint anónimo para crear administradores. Una segunda
inicialización se rechaza cuando ya existe una asignación al rol administrador.
Las altas posteriores se realizan desde la API o el frontend con permisos.

### Preparar la operación desde el frontend

1. Inicia sesión con el administrador; configura sedes, áreas, equipos, políticas
   de atención y servicios activos.
2. Crea usuarios, asígnales un área y sus roles; incorpora los técnicos a su
   equipo. Las asignaciones requieren integrantes activos y disponibles, con
   permiso para atender tickets.
3. El registro público asigna solamente el rol empleado. La administración de
   usuarios y roles permanece protegida.
4. `GET /api/Auth/sesion` devuelve roles y permisos vigentes para la navegación.
   La API vuelve a comprobar permisos y estado del usuario en cada JWT recibido.
5. El solicitante crea un ticket; el coordinador asigna un técnico; el técnico
   inicia y resuelve la atención; el solicitante confirma el cierre. Las acciones
   usan la versión vigente del ticket para detectar actualizaciones concurrentes.

Los endpoints `GET /api/IntegrantesEquipo/personas` y
`GET /api/Tickets/{id}/tecnicos-disponibles` proporcionan las opciones autorizadas
para los formularios. Las notificaciones del ciclo de tickets respetan las
preferencias y no publican comentarios internos al solicitante. Los adjuntos
de esta entrega almacenan metadatos; todavía no suben ni descargan archivos físicos.

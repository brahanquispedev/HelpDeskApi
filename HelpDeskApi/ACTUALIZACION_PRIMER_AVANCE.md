# Actualización del avance de Brahan — estilo Lab11

Este repositorio es el trabajo del equipo, diferente de la plantilla BrahanPRANK. Esta actualización adapta exclusivamente el avance ya implementado de Tickets, AsignacionesTickets y sus componentes compartidos.

## Base de datos

Se mantiene el esquema de 36 tablas y 2 vistas. No se crean tablas ni hace falta recrear helpdeskdb por este cambio de código. Los modelos y el contexto generados se conservan; HelpDeskDbContext.Custom.cs corrige las relaciones de asignaciones y conserva la configuración de concurrencia sin cambiar el SQL.

## Entradas y salidas

- Los Commands son clases con sus campos y validaciones directamente, como en Lab11.
- Las Queries de búsqueda reciben identificadores con nombre: IdTicket o IdAsignacionTicket.
- Los listados reciben paginación y los filtros aplicables directamente en el Query.
- Los DTOs están en HelpDesk.Domain/Dtos/{Entidad}, uno por archivo. Los compartidos están en Dtos/Common y los de autenticación en Dtos/Autenticacion.
- Los handlers construyen las respuestas con ResponseDto<T> y el DTO específico. No utilizan ILogger. CancellationToken solo se declara en Handle porque lo exige MediatR.

## Casos de uso incluidos en el avance

Tickets tiene Crear, Actualizar, Asignar, Iniciar, Resolver y Cerrar: 6 Commands. También tiene Obtener y Listar: 2 Queries. AsignacionesTickets tiene Obtener y Listar: 2 Queries; las asignaciones se crean y actualizan desde las acciones de Tickets.

Espera, reanudación, reapertura y cancelación quedan para una segunda entrega. La primera asignación completa el registro inicial del ticket; esta versión no ofrece reasignación.

## Persistencia compartida

El repositorio ofrece operaciones básicas y Query para LINQ. UnitOfWork obtiene repositorios y guarda mediante Complete(). Application usa EF Core para ToListAsync y CountAsync; sus dependencias se restauran con dotnet restore.

Los contratos de seguridad de Domain están en archivos separados. JwtService y PasswordService conservan JWT y hash. El registro automático de auditoría queda para otra entrega.

## Estado de la API

Este avance todavía no incorpora controladores de negocio ni integra los servicios de HelpDesk en Program.cs; el arranque conserva WeatherForecast. Compilar correctamente verifica los componentes, pero no permite probar todavía el flujo completo por HTTP. Autenticación y los módulos de los demás integrantes se incorporan en sus respectivos avances.

## Compilar

Desde la raíz del repositorio:

```powershell
dotnet restore .\HelpDeskApi\HelpDeskApi.sln
dotnet build .\HelpDeskApi\HelpDeskApi.sln
```

Los nombres de los proyectos son HelpDesk.Application, HelpDesk.Domain, HelpDesk.Infrastructure y HelpDeskApi.

# HelpDesk API — estructura base del equipo

Repositorio de trabajo del proyecto HelpDesk. El primer commit contiene la estructura compartida y el scaffolding de PostgreSQL para que los ocho integrantes implementen sus entidades desde una misma base.

## Qué incluye esta base

```text
HelpDeskApi/
├── HelpDeskApi.sln
├── HelpDeskApi/              Proyecto de entrada de la API
├── HelpDesk.Application/     Capa para comandos, consultas, handlers y DTOs
├── HelpDesk.Domain/          Entidades generadas por scaffolding
└── HelpDesk.Infrastructure/  DbContext y dependencias de persistencia
```

- Cuatro proyectos .NET 10 con sus referencias.
- Paquetes de EF Core, PostgreSQL, MediatR, JWT y Swagger declarados en los proyectos.
- Modelos en `HelpDesk.Domain/Models` y contexto en `HelpDesk.Infrastructure/Data`.
- Configuración inicial de ASP.NET Core.

Los modelos corresponden al diseño de 36 tablas y 2 vistas. Hay 34 clases porque cuatro tablas de relación son representadas por EF dentro del contexto, sin clase independiente.

**Esta estructura aún no implementa los CRUD, handlers, login/JWT ni middleware del proyecto final.** Tener sus librerías instaladas no significa que esas funcionalidades estén implementadas. `Program.cs` conserva el ejemplo inicial de ASP.NET Core; Swagger UI tampoco está configurado todavía.

## Clonar y compilar

Instalar Git y .NET SDK 10.0. Para trabajar con la base se necesita PostgreSQL 16 o superior y el script SQL de 36 tablas acordado por el equipo.

```powershell
git clone https://github.com/brahanquispedev/HelpDeskApi.git
cd HelpDeskApi
dotnet restore .\HelpDeskApi\HelpDeskApi.sln
dotnet build .\HelpDeskApi\HelpDeskApi.sln
```

Para ejecutar la aplicación inicial:

```powershell
dotnet run --project .\HelpDeskApi\HelpDeskApi --launch-profile http
```

El perfil actual utiliza `http://localhost:5086`; el ejemplo inicial está en `/weatherforecast`. Los endpoints del negocio se incorporarán en las contribuciones de cada integrante.

La base de datos no se crea al ejecutar la aplicación. Cada integrante debe crear su base local y aplicar el SQL antes de utilizar funciones de persistencia. Las conexiones y claves JWT se configuran localmente mediante User Secrets o variables de entorno y no se suben a Git.

## Cómo trabajaremos con las ramas

**`main` contiene la base común y las contribuciones ya integradas.** Cada integrante crea una rama para su grupo de entidades, sube sus commits y abre un Pull Request hacia `main`.

Ejemplo para un integrante:

```powershell
git switch main
git pull --ff-only origin main
git switch -c feature/gianfranco-cambios
```

Después de implementar y verificar su parte:

```powershell
# Revisar qué cambió.
git status

# Seleccionar archivos de su grupo; sustituir esta ruta por la que corresponda.
git add .\HelpDeskApi\HelpDesk.Application\UseCases\Cambios\

git commit -m "feat: agregar casos de uso de cambios"
git push -u origin feature/gianfranco-cambios
```

Agregar también los DTOs, controladores y demás archivos de su grupo que correspondan. El ejemplo de carpeta `UseCases/Cambios` se utilizará cuando el responsable la cree; no forma parte de la base inicial.

En GitHub, abrir **Pull requests → New pull request**, seleccionando `main` como destino y su rama como origen. Un compañero revisa el contenido y la compilación antes de integrar. Usar la cuenta y autoría propia para que los commits permitan identificar el aporte de cada integrante.

| Integrante | Rama sugerida | Grupo funcional |
|---|---|---|
| Diego Medina | `feature/diego-seguridad` | Usuarios, roles, permisos y autenticación |
| Gianfranco Paolo | `feature/gianfranco-cambios` | Cambios, tareas y aprobaciones |
| Fernando Aquima | `feature/fernando-activos` | Activos y asignaciones |
| Leticia Maldonado | `feature/leticia-conocimiento` | Conocimiento y problemas recurrentes |
| Frank Oviedo | `feature/frank-comunicacion` | Comentarios, adjuntos y notificaciones |
| Stiven Yerai | `feature/stiven-organizacion` | Sedes, áreas y equipos de soporte |
| Ederson Vilca | `feature/ederson-servicios` | Servicios, políticas y encuestas |
| Brahan Quispe | `feature/brahan-tickets` | Tickets, reportes, auditoría e integración |

Los propietarios del repositorio deben invitar a los compañeros como colaboradores para que puedan subir ramas. Este commit no configura invitaciones ni reglas de protección de `main`.

## Coordinación de archivos compartidos

- Coordinar con Brahan los cambios al `.sln`, los `.csproj`, el contexto y el scaffolding.
- No regenerar todos los modelos por una modificación de un handler.
- Coordinar la seguridad compartida con Diego.
- No subir `bin`, `obj`, `.idea`, credenciales, tokens ni configuración privada.
- Mantener los cambios de esquema en scripts SQL versionados.

El repositorio de referencia y este repositorio de trabajo tienen propósitos distintos. Cada contribución debe indicar qué se incorporó, qué se adaptó y cómo se verificó.

## Pruebas

```powershell
dotnet test .\HelpDeskApi\HelpDesk.Tests\HelpDesk.Tests.csproj
```

## Health check

`GET /health` devuelve el estado de la API.

# HelpDeskFrontend

Frontend en React, TypeScript, Vite y React Router para la API del equipo. Incluye gestión administrativa de cuentas, gestión de organización y catálogos, el ciclo de tickets, seguimiento e indicadores. La API comprueba los permisos de cada operación.

## Arranque en Windows

Inicia PostgreSQL y arranca la API del repositorio con el perfil `http`.

```powershell
Set-Location 'C:\Users\ASUS\Pictures\HelpDesk\HelpDeskApi'
dotnet run --project .\HelpDeskApi\HelpDeskApi --launch-profile http
```

En otra terminal, usa Node.js 20.19+ o 22.12+.

```powershell
Set-Location 'C:\Users\ASUS\Pictures\HelpDesk\HelpDeskApi\HelpDeskFrontend'
npm ci
npm run dev
```

Abre [el frontend local](http://localhost:5173). Usa una cuenta real de desarrollo. La API por defecto es `http://localhost:5086`. Si cambia, copia `.env.example` a `.env.local` y ajusta `VITE_API_BASE_URL`. El backend debe permitir el origen exacto del frontend mediante `Cors:AllowedOrigins`.

El JWT se mantiene en memoria. Si recargas la página, vuelve a ingresar. El frontend muestra los errores que devuelve la API. La versión actual del ticket se envía con cada acción y, si hay un conflicto, puedes recargar el detalle antes de continuar.

## Preparar el recorrido

Si todavía no existe un administrador, inicialízalo una vez desde la raíz del repositorio con PostgreSQL y los secretos locales configurados:

```powershell
.\HelpDeskApi\scripts\Crear-Administrador.ps1 -NombreUsuario admin -Correo admin@ejemplo.test
```

1. Como administrador, abre **Gestión**. Crea una sede, un área de esa sede y un equipo de soporte.
2. Como administrador, en **Usuarios**, crea un empleado, un coordinador y un técnico con roles y áreas asignados. La interfaz no ofrece registro público. La API actual todavía expone `POST /api/Auth/registro`; si las cuentas deben crearse solo desde administración, hay que deshabilitar ese endpoint antes de publicar el servicio. En **Gestión → Integrantes**, agrega al coordinador y al técnico al equipo. Marca a la primera cuenta como coordinadora del equipo.
3. En **Gestión**, crea o reutiliza una categoría, crea un servicio activo para el equipo y configura una política activa para la prioridad `media`. El formulario de tickets ofrece las prioridades habilitadas.
4. Como empleado, crea un ticket. Como coordinador, ábrelo y selecciona un técnico disponible. Como técnico, inicia la atención y resuelve. Como solicitante, cierra el ticket y registra su evaluación.

La interfaz obtiene los permisos efectivos mediante `GET /api/Auth/sesion`. Después de cambiar los permisos de una cuenta, inicia una nueva sesión para actualizar sus opciones de menú. La API aplica los cambios en el servidor al validar cada solicitud.

Los módulos de seguimiento permiten consultar lo que ya registra la API. Los adjuntos permanecen ocultos en esta interfaz. El envío real de correo, el restablecimiento por correo, la carga de archivos y la auditoría automática requieren implementar esos servicios en el backend.

**Avisos** recibe las notificaciones de asignación, atención, resolución, cierre y comentarios públicos. Los coordinadores del equipo reciben un aviso al entrar un ticket nuevo. Cada cuenta puede ajustar los avisos automáticos desde **Mi cuenta**. Un administrador también puede enviar un aviso directo a una cuenta activa.

`npm run build` verifica tipos y genera `dist`. Si publicas esa carpeta con otro servidor, configura la redirección de rutas de la SPA a `index.html`.


## Despliegue del frontend

El repositorio contiene el frontend y el backend. Publicar React como un sitio
estático no publica automáticamente la API .NET ni PostgreSQL.

Configuración del servicio de hosting estático:

| Campo | Valor |
|---|---|
| Directorio raíz del frontend | `HelpDeskFrontend` |
| Instalación | `npm ci` |
| Compilación | `npm run build` |
| Directorio de publicación, relativo a esa raíz | `dist` |
| Variable de compilación | `VITE_API_BASE_URL=https://dominio-publico-de-la-api` |

Si el proveedor ejecuta desde la raíz del repositorio, adaptar los comandos a
`npm --prefix HelpDeskFrontend ci` y `npm --prefix HelpDeskFrontend run build`,
y publicar `HelpDeskFrontend/dist`. No combinar ambas configuraciones de raíz.

`VITE_API_BASE_URL` apunta a la raíz de la API, sin agregar `/api`, porque los
clientes ya incluyen ese prefijo. El dominio mostrado arriba es un ejemplo;
reemplazarlo por el real antes de compilar. No usar localhost en producción.
Vite incorpora esa variable al compilar: después de cambiarla, reconstruir y
desplegar de nuevo. No colocar contraseñas, claves JWT ni cadenas de conexión
en variables `VITE_*`, porque forman parte del código público del navegador.

El hosting debe servir `index.html` para rutas de aplicación como `/login`,
`/tickets` o `/gestion/sedes`, conservando la URL. Es una reescritura interna de
SPA; las solicitudes de recursos existentes deben conservar sus archivos.
La regla concreta se configura al elegir el proveedor. `npm run dev` y
`npm run preview` se utilizan localmente, no como servidor de producción.

En el servicio de la API configurar:

- PostgreSQL accesible y el esquema inicial aplicado.
- `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Jwt__Issuer` y
  `Jwt__Audience` mediante secretos o variables del servidor.
- `Cors__AllowedOrigins__0=https://dominio-publico-del-frontend`, sin barra final.
- HTTPS para API y frontend. No reutilizar claves de desarrollo en producción.

El origen de frontend y la URL de API son direcciones diferentes cuando se
alojan en servicios separados. CORS debe contener el origen del frontend;
`VITE_API_BASE_URL` debe contener la dirección de la API.

Los secretos locales de .NET y `.env.local` no viajan con un clon ni al hosting.
No subir `node_modules`, `dist`, archivos `.env.local`, contraseñas o tokens.
`package-lock.json` sí se versiona para instalar las dependencias con `npm ci`.

Antes de abrir acceso público, resolver la política del registro de empleados,
cambiar las credenciales temporales utilizadas durante desarrollo y comprobar
login, permisos y el ciclo completo de un ticket contra la API desplegada.
En la versión actual el JWT se mantiene en memoria; recargar la página exige
iniciar sesión otra vez. No está implementada la recuperación por correo.

Referencias: [despliegue estático de Vite](https://vite.dev/guide/static-deploy.html)
y [variables de entorno en Vite](https://vite.dev/guide/env-and-mode.html).

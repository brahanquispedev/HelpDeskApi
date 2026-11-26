import { can, isAdmin } from './api'
import type { Session } from './api'

export type Row = Record<string, string | number | boolean | null>
type Reference = { route: string; id: string; label: string; enabled?: string }
export type Field = {
  name: string; label: string; kind?: 'text' | 'long' | 'number' | 'boolean' | 'select' | 'datetime'; required?: boolean
  max?: number; initial?: string | number | boolean; editOnly?: boolean; createOnly?: boolean
  ref?: Reference; choices?: Record<string, string>
}
export type Catalog = {
  slug: string; title: string; route: string; ids: string[]; columns: Field[]; fields?: Field[]
  update?: boolean; remove?: 'Desactivar' | 'Eliminar' | 'Quitar'; writePermission?: string; readPermission?: string
  hidden?: boolean
  actions?: { label: string; suffix: string; fields: Field[]; when?: (row: Row) => boolean }[]
}

const text = (name: string, label: string, required = true, max = 150): Field => ({ name, label, required, max })
const long = (name: string, label: string, required = false): Field => ({ name, label, kind: 'long', required, max: 10000 })
const flag = (name: string, label: string, initial = false): Field => ({ name, label, kind: 'boolean', initial })
const positive = (name: string, label: string, initial?: number): Field => ({ name, label, kind: 'number', required: true, initial })
const choices = (name: string, label: string, options: Record<string, string>, initial?: string): Field => ({ name, label, kind: 'select', choices: options, initial, required: true })
const relation = (name: string, label: string, route: string, id: string, display = 'nombre', enabled?: string, required = true): Field => ({ name, label, kind: 'select', ref: { route: `/api/${route}`, id, label: display, enabled }, required })
const active = flag('activo', 'Activo', true)
const name = text('nombre', 'Nombre', true, 100)
const desc = long('descripcion', 'Descripción')
const org = 'organizacion.administrar'
const priorities = { baja: 'Baja', media: 'Media', alta: 'Alta', critica: 'Crítica' }
const priority = choices('prioridad', 'Prioridad', priorities, 'media')
const site = relation('idSede', 'Sede', 'Sedes', 'idSede', 'nombre', 'activo')
const team = relation('idEquipo', 'Equipo', 'EquiposSoporte', 'idEquipo', 'nombre', 'activo')
const serviceCategory = relation('idCategoriaServicio', 'Categoría', 'CategoriasServicios', 'idCategoriaServicio', 'nombre', 'activo')
const teamPerson = relation('idUsuario', 'Persona', 'IntegrantesEquipo/personas', 'idUsuario')
const role = relation('idRol', 'Rol', 'Roles', 'idRol')
const permission = relation('idPermiso', 'Permiso', 'Permisos', 'idPermiso', 'codigo')
const user = (key: string, label: string, required = true) => relation(key, label, 'Usuarios', 'idUsuario', 'nombreUsuario', 'activo', required)
const ticket = (required = true) => relation('idTicket', 'Ticket', 'Tickets', 'idTicket', 'asunto', undefined, required)
const assetKind = relation('idTipoActivo', 'Tipo', 'TiposActivos', 'idTipoActivo', 'nombre', 'activo')
const asset = relation('idActivo', 'Activo', 'Activos', 'idActivo', 'codigoPatrimonial')
const change = (required = true) => relation('idCambio', 'Cambio', 'Cambios', 'idCambio', 'titulo', undefined, required)
const articleCategory = relation('idCategoriaConocimiento', 'Categoría', 'CategoriasConocimiento', 'idCategoriaConocimiento', 'nombre', 'activo')
const article = relation('idArticulo', 'Artículo', 'ArticulosConocimiento', 'idArticulo', 'titulo')
const problem = relation('idProblema', 'Problema', 'Problemas', 'idProblema', 'titulo')
const date = (name: string, label: string): Field => ({ name, label, kind: 'datetime' })

const settings: Catalog[] = [
  { slug: 'sedes', title: 'Sedes', route: 'Sedes', ids: ['idSede'], fields: [name, text('direccion', 'Dirección', false, 255), { ...active, editOnly: true }], columns: [name, text('direccion', 'Dirección'), active], update: true, remove: 'Desactivar', writePermission: org },
  { slug: 'areas', title: 'Áreas', route: 'Areas', ids: ['idArea'], fields: [name, site, { ...active, editOnly: true }], columns: [name, site, active], update: true, remove: 'Desactivar', writePermission: org },
  { slug: 'equipos', title: 'Equipos de soporte', route: 'EquiposSoporte', ids: ['idEquipo'], fields: [name, { ...desc, max: 500 }, { ...active, editOnly: true }], columns: [name, desc, active], update: true, remove: 'Desactivar', writePermission: org },
  { slug: 'integrantes', title: 'Integrantes', route: 'IntegrantesEquipo', ids: ['idEquipo', 'idUsuario'], fields: [{ ...team, createOnly: true }, { ...teamPerson, createOnly: true }, flag('esCoordinador', 'Coordina el equipo'), { ...flag('disponible', 'Disponible', true), editOnly: true }, { ...active, editOnly: true }], columns: [team, teamPerson, flag('esCoordinador', 'Coordina'), flag('disponible', 'Disponible'), active], update: true, remove: 'Desactivar', writePermission: org },
  { slug: 'categorias-servicios', title: 'Categorías de servicio', route: 'CategoriasServicios', ids: ['idCategoriaServicio'], fields: [name, desc, active], columns: [name, desc, active], update: true, remove: 'Eliminar' },
  { slug: 'servicios', title: 'Servicios', route: 'Servicios', ids: ['idServicio'], fields: [name, { ...desc, required: true }, serviceCategory, { ...team, name: 'idEquipoResponsable' }, flag('requiereAprobacion', 'Requiere aprobación'), active], columns: [name, serviceCategory, { ...team, name: 'idEquipoResponsable' }, active], update: true, remove: 'Eliminar' },
  { slug: 'politicas', title: 'Políticas de atención', route: 'PoliticasAtencion', ids: ['idPolitica'], fields: [priority, positive('minutosPrimeraRespuesta', 'Primera respuesta (minutos)', 30), positive('minutosResolucion', 'Resolución (minutos)', 120), active], columns: [priority, positive('minutosPrimeraRespuesta', 'Respuesta (min)'), positive('minutosResolucion', 'Resolución (min)'), active], update: true, remove: 'Eliminar' },
  { slug: 'roles', title: 'Roles', route: 'Roles', ids: ['idRol'], fields: [name, desc], columns: [name, desc], update: true, remove: 'Eliminar' },
  { slug: 'permisos', title: 'Permisos', route: 'Permisos', ids: ['idPermiso'], fields: [text('codigo', 'Código'), text('modulo', 'Módulo'), { ...desc, required: true }], columns: [text('codigo', 'Código'), text('modulo', 'Módulo'), desc], update: true, remove: 'Eliminar' },
  { slug: 'roles-permisos', title: 'Permisos por rol', route: 'RolesPermisos', ids: ['idRol', 'idPermiso'], fields: [role, permission], columns: [role, permission], remove: 'Quitar' },
]

const operations: Catalog[] = [
  { slug: 'tipos-activos', title: 'Tipos de activo', route: 'TiposActivos', ids: ['idTipoActivo'], fields: [name, desc, active], columns: [name, desc, active], update: true, remove: 'Eliminar' },
  { slug: 'activos', title: 'Inventario de activos', route: 'Activos', ids: ['idActivo'], fields: [name, text('codigoPatrimonial', 'Código patrimonial'), assetKind, site, relation('idArea', 'Área de la sede', 'Areas', 'idArea', 'nombre', 'activo', false), text('numeroSerie', 'Número de serie', false), text('marca', 'Marca', false), text('modelo', 'Modelo', false), { ...choices('estado', 'Estado inicial', { disponible: 'Disponible', mantenimiento: 'Mantenimiento' }, 'disponible'), createOnly: true }], columns: [name, text('codigoPatrimonial', 'Código patrimonial'), text('estado', 'Estado')], update: true, readPermission: 'activos.consultar', writePermission: 'activos.administrar', actions: [
    { label: 'Enviar a mantenimiento', suffix: 'estado', fields: [choices('estado', 'Estado', { mantenimiento: 'Mantenimiento' }, 'mantenimiento')], when: row => row.estado === 'disponible' },
    { label: 'Volver a disponible', suffix: 'estado', fields: [choices('estado', 'Estado', { disponible: 'Disponible' }, 'disponible')], when: row => row.estado === 'mantenimiento' },
    { label: 'Retirar', suffix: 'estado', fields: [choices('estado', 'Estado', { retirado: 'Retirado' }, 'retirado')], when: row => row.estado === 'disponible' || row.estado === 'mantenimiento' },
  ] },
  { slug: 'asignaciones-activos', title: 'Asignaciones de activos', route: 'AsignacionesActivos', ids: ['idAsignacion'], fields: [asset, user('idUsuarioResponsable', 'Responsable'), long('observaciones', 'Observaciones')], columns: [asset, user('idUsuarioResponsable', 'Responsable'), date('fechaAsignacion', 'Asignación'), date('fechaDevolucion', 'Devolución')], actions: [{ label: 'Registrar devolución', suffix: 'devolver', fields: [long('motivo', 'Motivo', true)], when: row => !row.fechaDevolucion }] },
  { slug: 'problemas', title: 'Problemas', route: 'Problemas', ids: ['idProblema'], fields: [text('titulo', 'Título'), { ...desc, required: true }, user('idResponsable', 'Responsable'), choices('estado', 'Estado', { abierto: 'Abierto', en_investigacion: 'En investigación', resuelto: 'Resuelto', cerrado: 'Cerrado' }, 'abierto'), long('causaRaiz', 'Causa raíz'), long('solucionTemporal', 'Solución temporal'), long('solucionDefinitiva', 'Solución definitiva')], columns: [text('titulo', 'Título'), text('estado', 'Estado'), desc], readPermission: 'problemas.gestionar', update: true },
  { slug: 'cambios', title: 'Cambios', route: 'Cambios', ids: ['idCambio'], fields: [text('titulo', 'Título'), { ...desc, required: true }, user('idResponsable', 'Responsable'), choices('riesgo', 'Riesgo', { bajo: 'Bajo', medio: 'Medio', alto: 'Alto' }, 'bajo'), long('planEjecucion', 'Plan de ejecución', true), long('planReversion', 'Plan de reversión', true), flag('requiereAprobacion', 'Requiere aprobación'), date('fechaProgramada', 'Fecha programada'), long('resultado', 'Resultado')], columns: [text('titulo', 'Título'), text('riesgo', 'Riesgo'), text('estado', 'Estado')], readPermission: 'cambios.gestionar', update: true },
  { slug: 'tareas-cambios', title: 'Tareas de cambios', route: 'TareasCambios', ids: ['idTareaCambio'], fields: [change(), user('idResponsable', 'Responsable', false), positive('orden', 'Orden', 1), text('titulo', 'Título'), desc, choices('estado', 'Estado', { pendiente: 'Pendiente', en_ejecucion: 'En ejecución', completada: 'Completada', cancelada: 'Cancelada' }, 'pendiente')], columns: [text('titulo', 'Título'), text('estado', 'Estado'), positive('orden', 'Orden')], readPermission: 'cambios.gestionar', update: true, remove: 'Eliminar' },
  { slug: 'categorias-conocimiento', title: 'Categorías de conocimiento', route: 'CategoriasConocimiento', ids: ['idCategoriaConocimiento'], fields: [name, desc, active], columns: [name, desc, active], update: true, remove: 'Eliminar' },
  { slug: 'conocimiento', title: 'Base de conocimiento', route: 'ArticulosConocimiento', ids: ['idArticulo'], fields: [text('titulo', 'Título'), articleCategory, long('contenido', 'Contenido', true), flag('esInterno', 'Artículo interno')], columns: [text('titulo', 'Título'), text('estado', 'Estado'), long('contenido', 'Contenido')], readPermission: 'conocimiento.consultar', update: true, remove: 'Eliminar' },
  { slug: 'aprobaciones', title: 'Aprobaciones', route: 'Aprobaciones', ids: ['idAprobacion'], fields: [ticket(false), change(false), user('idAprobador', 'Aprobador'), positive('orden', 'Orden', 1), long('motivo', 'Motivo', true), long('comentarioDecision', 'Comentario de decisión')], columns: [text('estado', 'Estado'), long('motivo', 'Motivo'), long('comentarioDecision', 'Decisión')], readPermission: 'aprobaciones.decidir', update: true },
  { slug: 'activos-tickets', title: 'Activos vinculados a tickets', route: 'TicketsActivos', ids: ['idTicket', 'idActivo'], fields: [ticket(), asset], columns: [ticket(), asset], remove: 'Quitar' },
  { slug: 'problemas-tickets', title: 'Problemas vinculados a tickets', route: 'ProblemasTickets', ids: ['idProblema', 'idTicket'], fields: [problem, ticket()], columns: [problem, ticket()], remove: 'Quitar' },
  { slug: 'cambios-tickets', title: 'Cambios vinculados a tickets', route: 'CambiosTickets', ids: ['idCambio', 'idTicket'], fields: [change(), ticket()], columns: [change(), ticket()], remove: 'Quitar' },
  { slug: 'articulos-tickets', title: 'Artículos vinculados a tickets', route: 'TicketsArticulos', ids: ['idTicket', 'idArticulo'], fields: [ticket(), article], columns: [ticket(), article], remove: 'Quitar' },
  { slug: 'adjuntos', hidden: true, title: 'Metadatos de adjuntos', route: 'Adjuntos', ids: ['idAdjunto'], fields: [ticket(), text('nombreArchivo', 'Nombre del archivo', true, 255), text('tipoArchivo', 'Tipo MIME'), positive('tamanoBytes', 'Tamaño (bytes)'), flag('esInterno', 'Adjunto interno')], columns: [ticket(), text('nombreArchivo', 'Archivo'), positive('tamanoBytes', 'Tamaño (bytes)'), flag('esInterno', 'Interno')], update: true, remove: 'Eliminar' },
]

const monitoring: Catalog[] = [
  { slug: 'auditoria', title: 'Auditoría', route: 'Auditoria', ids: ['idAuditoria'], columns: [text('accion', 'Acción'), text('tablaAfectada', 'Recurso'), desc, date('fecha', 'Fecha')], readPermission: 'auditoria.consultar' },
  { slug: 'recuperaciones', title: 'Solicitudes de recuperación', route: 'RecuperacionesClave', ids: ['idRecuperacion'], columns: [user('idUsuario', 'Usuario'), date('fechaSolicitud', 'Solicitud'), date('fechaVencimiento', 'Vencimiento'), date('fechaUso', 'Uso')] },
  { slug: 'envios', title: 'Historial de envíos', route: 'EnviosNotificaciones', ids: ['idEnvio'], columns: [text('correoDestino', 'Destino'), text('estado', 'Estado'), positive('intentos', 'Intentos'), date('fechaEnvio', 'Enviado')] },
]

export const groups = [
  { title: 'Organización', entries: settings.slice(0, 4) },
  { title: 'Catálogo y atención', entries: settings.slice(4, 7) },
  { title: 'Control de acceso', entries: settings.slice(7) },
  { title: 'Operaciones', entries: operations },
  { title: 'Seguimiento', entries: monitoring },
]

export function mayRead(session: Session | null, config: Catalog) {
  if (config.hidden) return false
  return isAdmin(session) || Boolean(config.readPermission && can(session, config.readPermission))
    || Boolean(config.writePermission && can(session, config.writePermission))
}

export function mayWrite(session: Session | null, config: Catalog) {
  if (config.hidden) return false
  return isAdmin(session) || Boolean(config.writePermission && can(session, config.writePermission))
}

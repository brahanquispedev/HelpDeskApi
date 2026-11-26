export const apiBase = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5086').replace(/\/+$/, '')

interface ResponseDto<T> {
  data: T | null
  mensaje?: string | null
}

export interface Page<T> {
  items: T[]
  pagina: number
  tamano: number
  total: number
}

export interface Session {
  token: string
  fechaVencimiento: string
  idUsuario: string
  nombreUsuario: string
  roles: string[]
  permisos: string[]
}

export type Call = <T>(path: string, options?: RequestOptions) => Promise<T>

export function can(session: Session | null, permission: string) {
  return session?.permisos?.includes(permission) ?? false
}

export function isAdmin(session: Session | null) {
  return can(session, 'usuarios.administrar')
}

export async function collect<T>(call: Call, route: string, signal: AbortSignal): Promise<T[]> {
  const items: T[] = []
  for (let pagina = 1; ; pagina++) {
    const separator = route.includes('?') ? '&' : '?'
    const result = await call<Page<T> | T[]>(`${route}${separator}pagina=${pagina}&tamano=100`, { signal })
    if (Array.isArray(result)) return result
    items.push(...result.items)
    if (!result.items.length || pagina * result.tamano >= result.total) return items
  }
}

export interface Ticket {
  idTicket: string
  numero: number
  idSolicitante: string
  idServicio: string
  idEquipoAsignado: string
  idTecnicoAsignado: string | null
  asunto: string
  descripcion: string
  tipo: string
  prioridad: string
  estado: string
  fechaCreacion: string
  fechaActualizacion: string
  fechaLimiteResolucion: string
  solucion: string | null
  version: number
}

export interface Service {
  idServicio: string
  idEquipoResponsable: string
  nombre: string
  activo: boolean
}

export class ApiError extends Error {
  constructor(public readonly status: number, message: string) {
    super(message)
  }
}

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  signal?: AbortSignal
}

export async function request<T>(path: string, { method = 'GET', body, signal }: RequestOptions = {}, token?: string): Promise<T> {
  const headers = new Headers({ Accept: 'application/json' })
  if (body !== undefined) headers.set('Content-Type', 'application/json')
  if (token) headers.set('Authorization', `Bearer ${token}`)

  let response: Response
  try {
    response = await fetch(`${apiBase}${path}`, {
      method, headers, signal,
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch (error) {
    if (signal?.aborted) throw error
    throw new ApiError(0, 'No pudimos conectar con la API. Comprueba que esté encendida y que el origen del frontend esté autorizado.')
  }

  const result: ResponseDto<T> | null = await response.json().catch(() => null)
  if (!response.ok) {
    throw new ApiError(response.status, result?.mensaje || `La solicitud no se pudo completar (HTTP ${response.status}).`)
  }
  if (result?.data == null) throw new ApiError(response.status, 'La API devolvió una respuesta inesperada.')
  return result.data
}

export async function listServices(call: typeof request, signal: AbortSignal) {
  const services: Service[] = []
  for (let pagina = 1; ; pagina++) {
    const page = await call<Page<Service>>(`/api/Servicios?pagina=${pagina}&tamano=100`, { signal })
    services.push(...page.items.filter(service => service.activo))
    if (!page.items.length || pagina * page.tamano >= page.total) break
  }
  return services.sort((a, b) => a.nombre.localeCompare(b.nombre, 'es'))
}

export function message(error: unknown) {
  return error instanceof Error ? error.message : 'Ocurrió un error inesperado.'
}

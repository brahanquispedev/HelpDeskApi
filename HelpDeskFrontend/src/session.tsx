import { createContext, useCallback, useContext, useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { ApiError, request } from './api'
import type { RequestOptions, Session } from './api'

interface SessionContextValue {
  session: Session | null
  notice: string
  login: (nombreUsuario: string, clave: string) => Promise<void>
  logout: () => void
  call: <T>(path: string, options?: RequestOptions) => Promise<T>
}

const SessionContext = createContext<SessionContextValue | null>(null)

type StoredSession = Omit<Session, 'roles' | 'permisos'>
const storageKey = 'helpdesk.session'

function readStoredSession(): StoredSession | null {
  try {
    const value = JSON.parse(sessionStorage.getItem(storageKey) ?? 'null')
    if (value && typeof value.token === 'string' && value.token &&
        typeof value.idUsuario === 'string' && typeof value.nombreUsuario === 'string' &&
        Date.parse(value.fechaVencimiento) > Date.now()) return value
  } catch { /* El navegador puede bloquear el almacenamiento. */ }
  clearStoredSession()
  return null
}

function clearStoredSession() {
  try { sessionStorage.removeItem(storageKey) } catch { /* Sesión solo en memoria. */ }
}

export function SessionProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(null)
  const [notice, setNotice] = useState('')

  const [pending, setPending] = useState(readStoredSession)
  const [retry, setRetry] = useState(0)
  const [restoreError, setRestoreError] = useState(false)

  useEffect(() => {
    if (!pending) return
    const controller = new AbortController()
    request<Pick<Session, 'roles' | 'permisos'>>('/api/Auth/sesion', { signal: controller.signal }, pending.token)
      .then(access => {
        if (controller.signal.aborted) return
        setSession({ ...pending, ...access })
        setPending(null)
      })
      .catch(error => {
        if (controller.signal.aborted) return
        if (error instanceof ApiError && error.status === 401) {
          clearStoredSession()
          setNotice('Tu sesión ya no es válida. Vuelve a iniciar sesión.')
          setPending(null)
        } else {
          setRestoreError(true)
        }
      })
    return () => controller.abort()
  }, [pending, retry])

  const logout = useCallback(() => {
    clearStoredSession()
    setPending(null)
    setNotice('')
    setSession(null)
  }, [])

  async function login(nombreUsuario: string, clave: string) {
    const next = await request<Session>('/api/Auth/login', { method: 'POST', body: { nombreUsuario, clave } })
    setNotice('')
    const access = await request<Pick<Session, 'roles' | 'permisos'>>('/api/Auth/sesion', {}, next.token)
    const { token, fechaVencimiento, idUsuario, nombreUsuario: usuarioAutenticado } = next
    try {
      sessionStorage.setItem(storageKey, JSON.stringify({ token, fechaVencimiento, idUsuario, nombreUsuario: usuarioAutenticado }))
    } catch { /* El inicio de sesión también funciona sin almacenamiento. */ }
    setSession({ ...next, ...access })
  }

  useEffect(() => {
    if (!session) return
    const timer = window.setTimeout(() => {
      clearStoredSession()
      setNotice('Tu sesión venció. Vuelve a iniciar sesión.')
      setSession(null)
    }, Math.max(0, Date.parse(session.fechaVencimiento) - Date.now()))
    return () => window.clearTimeout(timer)
  }, [session])

  const call = useCallback(async <T,>(path: string, options?: RequestOptions) => {
    if (!session) throw new ApiError(401, 'Inicia sesión para continuar.')
    try {
      return await request<T>(path, options, session.token)
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        if (readStoredSession()?.token === session.token) clearStoredSession()
        setNotice('La sesión ya no es válida. Vuelve a iniciar sesión.')
        setSession(current => current?.token === session.token ? null : current)
      }
      throw error
    }
  }, [session])

  if (pending) return <main aria-live="polite">
    {restoreError ? <>
      <p>No se pudo comprobar la sesión.</p>
      <button onClick={() => { setRestoreError(false); setRetry(value => value + 1) }}>Reintentar</button>
      <button onClick={logout}>Volver al inicio de sesión</button>
    </> : <p>Comprobando sesión…</p>}
  </main>

  return <SessionContext.Provider value={{ session, notice, login, logout, call }}>{children}</SessionContext.Provider>
}

export function useSession() {
  const session = useContext(SessionContext)
  if (!session) throw new Error('SessionProvider es obligatorio.')
  return session
}

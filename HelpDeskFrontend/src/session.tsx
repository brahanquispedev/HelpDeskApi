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

export function SessionProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(null)
  const [notice, setNotice] = useState('')

  const logout = useCallback(() => {
    setNotice('')
    setSession(null)
  }, [])

  async function login(nombreUsuario: string, clave: string) {
    const next = await request<Session>('/api/Auth/login', { method: 'POST', body: { nombreUsuario, clave } })
    setNotice('')
    const access = await request<Pick<Session, 'roles' | 'permisos'>>('/api/Auth/sesion', {}, next.token)
    setSession({ ...next, ...access })
  }

  useEffect(() => {
    if (!session) return
    const timer = window.setTimeout(() => {
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
        setNotice('La sesión ya no es válida. Vuelve a iniciar sesión.')
        setSession(current => current?.token === session.token ? null : current)
      }
      throw error
    }
  }, [session])

  return <SessionContext.Provider value={{ session, notice, login, logout, call }}>{children}</SessionContext.Provider>
}

export function useSession() {
  const session = useContext(SessionContext)
  if (!session) throw new Error('SessionProvider es obligatorio.')
  return session
}

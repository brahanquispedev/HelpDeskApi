import { ArrowRight, Bell, ChartNoAxesColumn, ChevronRight, Eye, EyeOff, LogOut, Plus, Settings, Ticket, UserRound, UsersRound } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, NavLink, Navigate, Outlet, Route, Routes, useLocation, useParams } from 'react-router'
import { can, isAdmin, message } from './api'
import { useSession } from './session'
import { Alert, Brand } from './ui'
import { TicketsPage } from './TicketsPage'
import { NewTicket } from './NewTicket'
import { TicketDetail } from './TicketDetail'
import { Users, UserRoute } from './Users'
import { CatalogRoute, Setup } from './CatalogPage'
import { Account, Notifications, Reports } from './OverviewPages'
import { groups, mayRead } from './catalog'

function Login() {
  const { session, login, notice } = useSession()
  const location = useLocation()
  const [error, setError] = useState('')
  const [pending, setPending] = useState(false)
  const [visible, setVisible] = useState(false)
  if (session) return <Navigate to="/tickets" replace />

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    setError('')
    setPending(true)
    try { await login(String(data.get('usuario')).trim(), String(data.get('clave'))) }
    catch (error) { setError(message(error)) }
    finally { setPending(false) }
  }

  return <main className="login-page">
    <section className="login-side">
      <Brand />
      <div className="login-content">
        <p className="login-eyebrow">BIENVENIDO A TU ESPACIO</p>
        <h1>Inicia sesión</h1>
        <p className="intro">Accede a tus solicitudes de soporte.</p>
        {(error || notice) && <Alert>{error || notice}</Alert>}
        {!error && !notice && location.state?.notice && <p className="success reload" role="status">{location.state.notice}</p>}
        <form className="form" onSubmit={submit}>
          <label>Usuario o correo<input name="usuario" autoComplete="username" maxLength={254} placeholder="usuario@empresa.com" required /></label>
          <label>Contraseña<span className="password-field"><input name="clave" autoComplete="current-password" type={visible ? 'text' : 'password'} minLength={10} maxLength={128} placeholder="Ingresa tu contraseña" required />
            <button className="icon-button" type="button" aria-label={visible ? 'Ocultar contraseña' : 'Mostrar contraseña'} onClick={() => setVisible(!visible)}>{visible ? <EyeOff size={18} /> : <Eye size={18} />}</button></span></label>
          <button className="button primary login-submit" disabled={pending}>{pending ? 'Ingresando…' : 'Ingresar'}<ArrowRight size={18} /></button>
        </form>
        <p className="login-access-note">Solicita tu cuenta al administrador de tu organización.</p>
      </div>
      <p className="login-foot">Mesa de ayuda · Soporte técnico</p>
    </section>
    <aside className="login-art" aria-hidden="true">
      <span className="login-art-label"><Ticket size={16} />Tu centro de soporte</span>
      <div className="login-art-copy">
        <h2>Todo tu soporte,<br />en un solo lugar.</h2>
        <p>Conecta con tu equipo, organiza tus solicitudes y sigue cada solución de principio a fin.</p>
        <div className="login-art-tags"><span><i />Solicitudes organizadas</span><span><i />Trabajo en equipo</span><span><i />Seguimiento claro</span></div>
      </div>
    </aside>
  </main>
}

function Workspace() {
  const { session, logout } = useSession()
  const { pathname } = useLocation()
  if (!session) return <Navigate to="/login" replace />
  const isDetail = pathname.startsWith('/tickets/')
  const isNew = pathname === '/tickets/nuevo'
  const isUsers = pathname.startsWith('/usuarios')
  const isManagement = pathname.startsWith('/gestion')
  const manage = groups.some(group => group.entries.some(entry => mayRead(session, entry)))
  const newTicket = isAdmin(session) || can(session, 'tickets.crear')
  const section = isUsers ? 'Usuarios' : isManagement ? 'Gestión' : pathname === '/reportes' ? 'Reportes' : pathname === '/avisos' ? 'Avisos' : pathname === '/cuenta' ? 'Mi cuenta' : 'Tickets'
  return <div className="workspace">
    <aside className="sidebar">
      <div className="brand-block"><Link to="/tickets" className="brand-link" aria-label="HelpDesk, ir a tickets"><Brand /></Link><span className="brand-caption">Centro de soporte</span></div>
      <nav aria-label="Navegación principal">
        <p className="nav-section">Principal</p>
        <NavLink to="/tickets" className={({ isActive }) => `nav-item ${isActive && !isNew ? 'active' : ''}`}><Ticket size={19} />Tickets</NavLink>
        {newTicket && <NavLink to="/tickets/nuevo" className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}><Plus size={19} />Nuevo ticket</NavLink>}
        {isAdmin(session) && <NavLink to="/usuarios" className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}><UsersRound size={19} />Usuarios</NavLink>}
        {manage && <NavLink to="/gestion" className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}><Settings size={19} />Gestión</NavLink>}
        <p className="nav-section">Espacio de trabajo</p>
        <NavLink to="/reportes" className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}><ChartNoAxesColumn size={19} />Reportes</NavLink>
        <NavLink to="/avisos" className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}><Bell size={19} />Avisos</NavLink>
        <NavLink to="/cuenta" className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}><UserRound size={19} />Mi cuenta</NavLink>
      </nav>
      <div className="sidebar-footer"><p>Tu equipo, siempre conectado.</p><button className="nav-item logout" onClick={logout}><LogOut size={18} />Cerrar sesión</button></div>
    </aside>
    <div className="workspace-body">
      <header className="topbar">
        <div className="topbar-heading"><span className="topbar-eyebrow">MESA DE AYUDA</span><div className="breadcrumb">{isDetail ? <><Link to="/tickets">Tickets</Link><ChevronRight size={15} /><span>{isNew ? 'Nuevo ticket' : 'Detalle'}</span></> : <span>{section}</span>}</div></div>
        <div className="account"><span className="avatar">{session.nombreUsuario.slice(0, 2).toUpperCase()}</span><div><strong>{session.nombreUsuario}</strong><small>{session.roles.join(' · ') || 'Usuario'}</small></div></div>
      </header>
      <main className="content"><Outlet /></main>
    </div>
  </div>
}

function DetailRoute() {
  const { id } = useParams()
  return id ? <TicketDetail key={id} id={id} /> : <Navigate to="/tickets" replace />
}

function AdminRoutes() {
  const { session } = useSession()
  return isAdmin(session) ? <Outlet /> : <Navigate to="/tickets" replace />
}

export function App() {
  return <Routes>
    <Route path="/login" element={<Login />} />
    <Route element={<Workspace />}>
      <Route path="/tickets" element={<TicketsPage />} />
      <Route path="/tickets/nuevo" element={<NewTicket />} />
      <Route path="/tickets/:id" element={<DetailRoute />} />
      <Route path="/gestion" element={<Setup />} />
      <Route path="/gestion/:section" element={<CatalogRoute />} />
      <Route path="/reportes" element={<Reports />} />
      <Route path="/avisos" element={<Notifications />} />
      <Route path="/cuenta" element={<Account />} />
      <Route element={<AdminRoutes />}>
        <Route path="/usuarios" element={<Users />} />
        <Route path="/usuarios/nuevo" element={<UserRoute />} />
        <Route path="/usuarios/:id" element={<UserRoute />} />
      </Route>
    </Route>
    <Route path="*" element={<Navigate to="/tickets" replace />} />
  </Routes>
}

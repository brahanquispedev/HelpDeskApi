import { Archive, ChartNoAxesColumn, CircleCheck, Clock3 } from 'lucide-react'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { isAdmin, message } from './api'
import { noticeEventLabel, noticeEvents } from './noticeEvents'
import { useSession } from './session'
import { Alert, Empty, Loading, formatDate } from './ui'

type Indicators = { total: number; pendientes: number; resueltos: number; cerrados: number }
type Notice = { idNotificacion: string; idTicket: string | null; titulo: string; mensaje: string; fechaCreacion: string; fechaLectura: string | null }
type Preference = { tipoEvento: string; recibirEnSistema: boolean; recibirPorCorreo: boolean }

function useRead<T>(path: string) {
  const { call } = useSession()
  const [data, setData] = useState<T | null>(null)
  const [error, setError] = useState('')
  const [version, setVersion] = useState(0)
  useEffect(() => {
    const abort = new AbortController()
    setError('')
    call<T>(path, { signal: abort.signal }).then(value => { if (!abort.signal.aborted) setData(value) })
      .catch(error => { if (!abort.signal.aborted) setError(message(error)) })
    return () => abort.abort()
  }, [call, path, version])
  return { data, error, reload: () => setVersion(value => value + 1) }
}

export function Reports() {
  const { data, error, reload } = useRead<Indicators>('/api/Reportes/indicadores')
  const indicators = [['Total de tickets', data?.total], ['Pendientes', data?.pendientes], ['Resueltos', data?.resueltos], ['Cerrados', data?.cerrados]] as const
  return <><div className="page-heading"><div><h1>Reportes</h1><p>Indicadores de los tickets a los que tienes acceso.</p></div><button className="button secondary" onClick={reload}>Actualizar</button></div>
    {error ? <Alert>{error}</Alert> : !data ? <Loading /> : <div className="management-grid report-grid">{indicators.map(([title, value], index) => {
      const Icon = [ChartNoAxesColumn, Clock3, CircleCheck, Archive][index]
      return <section className="panel detail-panel report-card" key={title}><span className="report-icon"><Icon size={19} aria-hidden="true" /></span><h2>{title}</h2><p className="report-value">{value}</p></section>
    })}</div>}</>
}

export function Notifications() {
  const { call, session } = useSession()
  const [unread, setUnread] = useState(false)
  const [pending, setPending] = useState('')
  const [actionError, setActionError] = useState('')
  const [composing, setComposing] = useState(false)
  const [sent, setSent] = useState(false)
  const { data, error, reload } = useRead<Notice[]>(`/api/Notificaciones?soloNoLeidas=${unread}`)

  async function mark(notice: Notice) {
    setPending(notice.idNotificacion)
    setActionError('')
    try { await call(`/api/notificaciones/${notice.idNotificacion}/marcar-leida`, { method: 'POST' }); reload() }
    catch (error) { setActionError(message(error)) }
    finally { setPending('') }
  }

  return <><div className="page-heading"><div><h1>Avisos</h1></div><div className="inline-actions">
    <label className="filter-toggle"><input type="checkbox" checked={unread} onChange={event => setUnread(event.target.checked)} />Solo sin leer</label>
    <button className="button secondary compact" onClick={reload}>Actualizar</button>
    {isAdmin(session) && !composing && <button className="button primary compact" onClick={() => { setSent(false); setComposing(true) }}>Enviar aviso</button>}
  </div></div>
    {sent && <p className="success reload" role="status">Aviso enviado.</p>}
    {composing && <NoticeComposer onCancel={() => setComposing(false)} onSent={() => { setComposing(false); setSent(true); reload() }} />}
    {(error || actionError) && <Alert>{error || actionError}</Alert>}
    {error && <button className="button secondary" onClick={reload}>Reintentar</button>}
    {!data ? (!error && <Loading />) : data.length ? <div className="notifications">{[...data].sort((a, b) => Date.parse(b.fechaCreacion) - Date.parse(a.fechaCreacion)).map(notice => <article className="panel detail-panel" key={notice.idNotificacion}>
      <div className="notice-heading"><h2>{notice.titulo}</h2>{!notice.fechaLectura && <span className="status status-nuevo">Sin leer</span>}</div><p className="ticket-description">{notice.mensaje}</p><p className="ticket-number">{formatDate(notice.fechaCreacion)}</p>
      <div className="inline-actions">{notice.idTicket && <Link className="button secondary compact" to={`/tickets/${notice.idTicket}`}>Ver ticket</Link>}{!notice.fechaLectura && <button className="button secondary compact" disabled={Boolean(pending)} onClick={() => void mark(notice)}>Marcar como leído</button>}</div>
    </article>)}</div> : <section className="panel"><Empty title="No tienes avisos">Los avisos disponibles aparecerán aquí.</Empty></section>}
  </>
}

function NoticeComposer({ onCancel, onSent }: { onCancel: () => void; onSent: () => void }) {
  const { call } = useSession()
  const { data, error, reload } = useRead<{ idUsuario: string; nombre: string }[]>('/api/IntegrantesEquipo/personas')
  const [pending, setPending] = useState(false)
  const [sendError, setSendError] = useState('')
  const [eventKey] = useState(() => crypto.randomUUID())

  async function send(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (pending) return
    const form = new FormData(event.currentTarget)
    const titulo = String(form.get('titulo')).trim()
    const mensaje = String(form.get('mensaje')).trim()
    if (!titulo || !mensaje) { setSendError('Completa el título y el mensaje.'); return }
    setPending(true)
    setSendError('')
    try {
      await call('/api/Notificaciones', { method: 'POST', body: {
        idUsuario: form.get('idUsuario'), titulo, mensaje, tipoEvento: 'aviso_administrativo', claveEvento: 'manual:' + eventKey,
      } })
      onSent()
    } catch (error) { setSendError(message(error)) }
    finally { setPending(false) }
  }

  return <section className="panel form-panel notice-composer"><form className="form" onSubmit={send}>
    <h2>Nuevo aviso</h2>
    {sendError && <Alert>{sendError}</Alert>}
    {error ? <><Alert>{error}</Alert><button type="button" className="button secondary compact" onClick={reload}>Recargar destinatarios</button></> : !data ? <Loading label="Cargando destinatarios…" /> : <label>Destinatario<select name="idUsuario" required defaultValue=""><option value="" disabled>Selecciona un usuario activo</option>{data.map(user => <option key={user.idUsuario} value={user.idUsuario}>{user.nombre}</option>)}</select></label>}
    <label>Título<input name="titulo" maxLength={200} required /></label>
    <label>Mensaje<textarea name="mensaje" maxLength={2000} rows={4} required /></label>
    <div className="form-actions"><button type="button" className="button secondary" disabled={pending} onClick={onCancel}>Cancelar</button><button className="button primary" disabled={pending || !data?.length}>{pending ? 'Enviando…' : 'Enviar aviso'}</button></div>
  </form></section>
}

export function Account() {
  const { call, logout, session } = useSession()
  const navigate = useNavigate()
  const [error, setError] = useState('')
  const [pending, setPending] = useState(false)

  async function change(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const claveActual = String(form.get('actual'))
    const claveNueva = String(form.get('nueva'))
    if (claveNueva !== String(form.get('confirmar'))) { setError('La confirmación no coincide con la contraseña nueva.'); return }
    setError('')
    setPending(true)
    try {
      await call('/api/Auth/cambiar-clave', { method: 'POST', body: { claveActual, claveNueva } })
      logout()
      navigate('/login', { replace: true, state: { notice: 'Contraseña actualizada. Ingresa con tu nueva contraseña.' } })
    } catch (error) { setError(message(error)) }
    finally { setPending(false) }
  }

  return <><div className="page-heading"><div><h1>Mi cuenta</h1><p>{session?.nombreUsuario}</p></div></div><section className="panel form-panel"><form className="form" onSubmit={change}><h2>Cambiar contraseña</h2>
    {error && <Alert>{error}</Alert>}
    <label>Contraseña actual<input type="password" name="actual" autoComplete="current-password" minLength={10} maxLength={128} required /></label>
    <label>Contraseña nueva<input type="password" name="nueva" autoComplete="new-password" minLength={10} maxLength={128} required /></label>
    <label>Confirmar contraseña<input type="password" name="confirmar" autoComplete="new-password" minLength={10} maxLength={128} required /></label>
    <div className="form-actions"><button className="button primary" disabled={pending}>{pending ? 'Guardando…' : 'Actualizar contraseña'}</button></div>
  </form></section>{session && <Preferences userId={session.idUsuario} />}</>
}

function Preferences({ userId }: { userId: string }) {
  const { call } = useSession()
  const { data, error, reload } = useRead<Preference[]>('/api/PreferenciasNotificaciones')
  const [selection, setSelection] = useState<Preference>()
  const [showForm, setShowForm] = useState(false)
  const [pending, setPending] = useState(false)
  const [notice, setNotice] = useState('')
  const [actionError, setActionError] = useState('')

  function edit(preference?: Preference) {
    setSelection(preference)
    setShowForm(true)
    setActionError('')
    setNotice('')
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const eventType = selection?.tipoEvento ?? String(form.get('tipoEvento')).trim()
    if (!eventType) { setActionError('Indica el tipo de evento.'); return }
    const preferences = { recibirEnSistema: form.has('recibirEnSistema'), recibirPorCorreo: form.has('recibirPorCorreo') }
    setPending(true)
    setActionError('')
    try {
      await call(selection ? `/api/PreferenciasNotificaciones/${userId}/${encodeURIComponent(eventType)}` : '/api/PreferenciasNotificaciones', {
        method: selection ? 'PUT' : 'POST', body: selection ? preferences : { ...preferences, idUsuario: userId, tipoEvento: eventType },
      })
      setShowForm(false)
      setNotice('Preferencia guardada.')
      reload()
    } catch (error) { setActionError(message(error)) }
    finally { setPending(false) }
  }

  async function remove(preference: Preference) {
    setPending(true)
    setActionError('')
    setNotice('')
    try {
      await call(`/api/PreferenciasNotificaciones/${userId}/${encodeURIComponent(preference.tipoEvento)}`, { method: 'DELETE' })
      setShowForm(false)
      setNotice('Preferencia eliminada.')
      reload()
    } catch (error) { setActionError(message(error)) }
    finally { setPending(false) }
  }

  return <section className="panel action-panel"><h2>Preferencias de avisos</h2>
    {error && <><Alert>{error}</Alert><button className="button secondary compact" onClick={reload}>Reintentar</button></>}
    {actionError && <Alert>{actionError}</Alert>}{notice && <p className="success reload" role="status">{notice}</p>}
    {data?.length ? <ul className="role-list">{data.map(preference => <li key={preference.tipoEvento}><div><strong>{noticeEventLabel(preference.tipoEvento)}</strong><p className="ticket-description">{preference.recibirEnSistema ? 'Avisos en la web' : 'Sin avisos en la web'}{preference.recibirPorCorreo ? ' · Preferencia de correo activa' : ''}</p></div><button className="button secondary compact" disabled={pending} onClick={() => edit(preference)}>Editar</button></li>)}</ul> : !error && (data ? <p className="intro">Recibes los avisos automáticos que te corresponden. Agrega una preferencia si quieres desactivar algún tipo de aviso.</p> : <Loading />)}
    {!showForm && data && <button className="button secondary" disabled={pending} onClick={() => edit()}>Agregar preferencia</button>}
    {showForm && <form className="form" key={selection?.tipoEvento ?? 'new'} onSubmit={save}>
      <label>Tipo de evento{selection ? <input readOnly value={noticeEventLabel(selection.tipoEvento)} /> : <select name="tipoEvento" required defaultValue=""><option value="" disabled>Selecciona un evento</option>{Object.entries(noticeEvents).filter(([key]) => !data?.some(item => item.tipoEvento === key)).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select>}</label>
      <label className="account-toggle"><input type="checkbox" name="recibirEnSistema" defaultChecked={selection?.recibirEnSistema ?? true} />Recibir en la web</label>
      <label className="account-toggle"><input type="checkbox" name="recibirPorCorreo" defaultChecked={selection?.recibirPorCorreo ?? false} />Guardar preferencia de correo</label>
      <div className="form-actions">{selection && <button className="button secondary" type="button" disabled={pending} onClick={() => void remove(selection)}>Eliminar preferencia</button>}<button className="button secondary" type="button" disabled={pending} onClick={() => setShowForm(false)}>Cancelar</button><button className="button primary" disabled={pending}>Guardar</button></div>
    </form>}
  </section>
}

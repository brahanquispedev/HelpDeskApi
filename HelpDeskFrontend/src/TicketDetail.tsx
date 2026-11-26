import { ArrowLeft, ArrowRight, CalendarDays, Clock3 } from 'lucide-react'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router'
import { ApiError, can, isAdmin, message } from './api'
import type { Session, Ticket } from './api'
import { useSession } from './session'
import { Alert, Loading, Priority, Status, formatDate, ticketCode } from './ui'
import { TicketActivity } from './TicketActivity'

type Action = 'asignar' | 'iniciar' | 'resolver' | 'cerrar'
const labels: Record<Action, string> = {
  asignar: 'Asignar ticket', iniciar: 'Iniciar atención', resolver: 'Marcar como resuelto', cerrar: 'Cerrar ticket',
}

function actionFor(ticket: Ticket, session: Session): Action | undefined {
  if (ticket.estado === 'nuevo' && (isAdmin(session) || can(session, 'asignaciones.administrar'))) return 'asignar'
  const technician = can(session, 'tickets.atender') && ticket.idTecnicoAsignado === session.idUsuario
  if (technician && ticket.estado === 'asignado') return 'iniciar'
  if (technician && ticket.estado === 'en_atencion') return 'resolver'
  if (ticket.idSolicitante === session.idUsuario && ticket.estado === 'resuelto' && can(session, 'tickets.confirmar_propios')) return 'cerrar'
}

export function TicketDetail({ id }: { id: string }) {
  const { call, session } = useSession()
  const [ticket, setTicket] = useState<Ticket | null>(null)
  const [attempt, setAttempt] = useState(0)
  const [loadError, setLoadError] = useState('')
  const [saveError, setSaveError] = useState('')
  const [conflict, setConflict] = useState(false)
  const [pending, setPending] = useState(false)
  const [saved, setSaved] = useState(false)
  const [technicians, setTechnicians] = useState<{ idUsuario: string; nombre: string }[] | null>(null)
  const [technicianError, setTechnicianError] = useState('')
  const [technicianAttempt, setTechnicianAttempt] = useState(0)
  const [editing, setEditing] = useState(false)
  const action = ticket && session ? actionFor(ticket, session) : undefined
  const canEdit = ticket?.estado === 'nuevo' && (isAdmin(session) || (can(session, 'tickets.crear') && ticket?.idSolicitante === session?.idUsuario))

  useEffect(() => {
    const abort = new AbortController()
    setTicket(null)
    setLoadError('')
    call<Ticket>(`/api/Tickets/${encodeURIComponent(id)}`, { signal: abort.signal })
      .then(ticket => { if (!abort.signal.aborted) setTicket(ticket) })
      .catch(error => { if (!abort.signal.aborted) setLoadError(message(error)) })
    return () => abort.abort()
  }, [id, call, attempt])

  useEffect(() => {
    if (action !== 'asignar') return
    const abort = new AbortController()
    setTechnicians(null)
    setTechnicianError('')
    call<{ idUsuario: string; nombre: string }[]>(`/api/Tickets/${encodeURIComponent(id)}/tecnicos-disponibles`, { signal: abort.signal })
      .then(result => { if (!abort.signal.aborted) setTechnicians(result) })
      .catch(error => { if (!abort.signal.aborted) setTechnicianError(message(error)) })
    return () => abort.abort()
  }, [id, call, action, attempt, technicianAttempt])

  async function editTicket(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!ticket || !canEdit || pending || conflict) return
    const form = new FormData(event.currentTarget)
    const asunto = String(form.get('asunto')).trim()
    const descripcion = String(form.get('descripcion')).trim()
    if (!asunto || !descripcion) { setSaveError('Completa el asunto y la descripción.'); return }
    setPending(true)
    setSaveError('')
    setSaved(false)
    try {
      const { idServicio, tipo, prioridad, version } = ticket
      setTicket(await call<Ticket>(`/api/Tickets/${id}`, { method: 'PUT', body: { asunto, descripcion, idServicio, tipo, prioridad, version } }))
      setEditing(false)
      setSaved(true)
    } catch (error) { setSaveError(message(error)); setConflict(error instanceof ApiError && error.status === 409) }
    finally { setPending(false) }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!action || !ticket || pending || conflict || (action === 'asignar' && !technicians?.length)) return
    const data = new FormData(event.currentTarget)
    const motivo = String(data.get('motivo')).trim()
    if (!motivo) { setSaveError('Indica el motivo de la acción.'); return }
    const body = { version: ticket.version, motivo, ...(action === 'asignar' ? { idTecnico: String(data.get('idTecnico')).trim() } : {}) }
    setSaveError('')
    setSaved(false)
    setPending(true)
    try {
      setTicket(await call<Ticket>(`/api/Tickets/${encodeURIComponent(id)}/${action}`, { method: 'POST', body }))
      setSaved(true)
    } catch (error) {
      setSaveError(message(error))
      setConflict(error instanceof ApiError && error.status === 409)
    } finally { setPending(false) }
  }

  function refresh() {
    setConflict(false)
    setSaveError('')
    setSaved(false)
    setEditing(false)
    setAttempt(attempt + 1)
  }

  return <>
    <Link className="back" to="/tickets"><ArrowLeft size={17} />Volver a tickets</Link>
    {loadError ? <div className="panel panel-message"><Alert>{loadError}</Alert><button className="button secondary" onClick={refresh}>Reintentar</button></div>
      : !ticket ? <Loading label="Cargando detalle…" />
      : <>
        <div className="page-heading detail-heading"><div><p className="ticket-number">Ticket {ticketCode(ticket.numero)}</p><h1>{ticket.asunto}</h1></div><Status value={ticket.estado} /></div>
        <div className="detail-grid">
          <section className="panel detail-panel"><h2>Descripción</h2><p className="ticket-description">{ticket.descripcion}</p>
            {canEdit && !editing && <button className="button secondary compact" onClick={() => { setEditing(true); setSaveError(''); setConflict(false) }}>Editar ticket</button>}
            {ticket.solucion && <div className="solution"><h2>Solución</h2><p className="ticket-description">{ticket.solucion}</p></div>}
          </section>
          <section className="panel detail-panel"><h2>Información</h2><dl className="facts">
            <div><dt>Prioridad</dt><dd><Priority value={ticket.prioridad} /></dd></div>
            <div><dt>Tipo</dt><dd>{ticket.tipo === 'incidente' ? 'Incidente' : 'Solicitud'}</dd></div>
            <div><dt><CalendarDays size={15} />Creación</dt><dd>{formatDate(ticket.fechaCreacion)}</dd></div>
            <div><dt><Clock3 size={15} />Plazo de resolución</dt><dd>{formatDate(ticket.fechaLimiteResolucion)}</dd></div>
          </dl></section>
        </div>
        {saved && <p className="success" role="status">El ticket se actualizó.</p>}
        {editing && canEdit && <section className="panel action-panel"><h2>Editar ticket</h2>{saveError && <Alert>{saveError}</Alert>}{conflict && <button className="button secondary reload" onClick={refresh}>Recargar ticket</button>}
          <form className="form" onSubmit={editTicket}><label>Asunto<input name="asunto" defaultValue={ticket.asunto} required maxLength={200} /></label><label>Descripción<textarea name="descripcion" defaultValue={ticket.descripcion} required rows={4} maxLength={10000} /></label><div className="form-actions"><button type="button" className="button secondary" disabled={pending} onClick={() => { setEditing(false); setSaveError('') }}>Cancelar</button><button className="button primary" disabled={pending || conflict}>Guardar cambios</button></div></form>
        </section>}
        {!editing && action && <section className="panel action-panel"><h2>{labels[action]}</h2>
          {saveError && <Alert>{saveError}</Alert>}
          {conflict && <button className="button secondary reload" onClick={refresh}>Recargar ticket</button>}
          <form key={`${action}-${ticket.version}`} className="form" onSubmit={submit}>
            {action === 'asignar' && <>{technicianError ? <><Alert>{technicianError}</Alert><button type="button" className="button secondary" onClick={() => setTechnicianAttempt(value => value + 1)}>Recargar técnicos</button></> : !technicians ? <Loading label="Cargando técnicos…" /> : !technicians.length ? <Alert>El equipo no tiene técnicos disponibles. Verifica sus integrantes y permisos.</Alert> : <label>Técnico<select name="idTecnico" defaultValue="" required><option value="" disabled>Selecciona un técnico</option>{technicians.map(technician => <option key={technician.idUsuario} value={technician.idUsuario}>{technician.nombre}</option>)}</select></label>}</>}
            <label>{action === 'resolver' ? 'Solución aplicada' : 'Motivo'}<textarea name="motivo" required maxLength={4000} rows={3} /></label>
            <div className="form-actions"><button className="button primary" disabled={pending || conflict || (action === 'asignar' && !technicians?.length)}>{pending ? 'Guardando…' : labels[action]}<ArrowRight size={18} /></button></div>
          </form>
        </section>}
        <TicketActivity ticket={ticket} />
      </>}
  </>
}

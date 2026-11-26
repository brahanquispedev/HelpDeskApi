import { ArrowLeft, ArrowRight } from 'lucide-react'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router'
import { can, collect, isAdmin, listServices, message } from './api'
import type { Service, Ticket } from './api'
import { useSession } from './session'
import { Alert, Loading, priorityLabels } from './ui'

export function NewTicket() {
  const { call, session } = useSession()
  const allowed = can(session, 'tickets.crear') || isAdmin(session)
  const id = session?.idUsuario
  const navigate = useNavigate()
  const [services, setServices] = useState<Service[] | null>(null)
  const [catalogError, setCatalogError] = useState('')
  const [error, setError] = useState('')
  const [attempt, setAttempt] = useState(0)
  const [pending, setPending] = useState(false)
  const [priorities, setPriorities] = useState<string[]>([])
  const [hasArea, setHasArea] = useState(false)

  useEffect(() => {
    if (!id || !allowed) return
    const abort = new AbortController()
    setCatalogError('')
    setServices(null)
    Promise.all([listServices(call, abort.signal), collect<{ prioridad: string; activo: boolean }>(call, '/api/PoliticasAtencion', abort.signal),
      call<{ idArea: string | null }>(`/api/Usuarios/${id}`, { signal: abort.signal }),
      call<{ idArea: string }[]>('/api/Areas?soloActivos=true', { signal: abort.signal }),
      call<{ idEquipo: string }[]>('/api/EquiposSoporte?soloActivos=true', { signal: abort.signal })])
      .then(([services, policies, user, areas, teams]) => {
        if (abort.signal.aborted) return
        setServices(services.filter(service => teams.some(team => team.idEquipo === service.idEquipoResponsable)))
        setPriorities(policies.filter(policy => policy.activo).map(policy => policy.prioridad))
        setHasArea(areas.some(area => area.idArea === user.idArea))
      })
      .catch(error => { if (!abort.signal.aborted) setCatalogError(message(error)) })
    return () => abort.abort()
  }, [call, attempt, id, allowed])

  if (!allowed) return <Navigate to="/tickets" replace />

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (pending || !services?.length || !priorities.length || !hasArea) return
    const form = new FormData(event.currentTarget)
    const asunto = String(form.get('asunto')).trim()
    const descripcion = String(form.get('descripcion')).trim()
    if (!asunto || !descripcion) { setError('Escribe un asunto y una descripción.'); return }
    setPending(true)
    setError('')
    try {
      const ticket = await call<Ticket>('/api/Tickets', {
        method: 'POST',
        body: { asunto, descripcion, idServicio: form.get('idServicio'), tipo: form.get('tipo'), prioridad: form.get('prioridad') },
      })
      navigate(`/tickets/${ticket.idTicket}`, { replace: true })
    } catch (error) { setError(message(error)) }
    finally { setPending(false) }
  }

  return <>
    <Link className="back" to="/tickets"><ArrowLeft size={17} />Volver a tickets</Link>
    <div className="page-heading"><div><h1>Nuevo ticket</h1><p>Describe qué ocurre para que soporte pueda ayudarte.</p></div></div>
    <section className="panel form-panel">
      {catalogError ? <div className="panel-message"><Alert>{catalogError}</Alert><button className="button secondary" onClick={() => setAttempt(attempt + 1)}>Recargar servicios</button></div>
        : !services ? <Loading label="Cargando servicios…" />
        : <form className="form" onSubmit={submit}>
          {error && <Alert>{error}</Alert>}
          {!services.length && <Alert>El catálogo no tiene servicios activos. {isAdmin(session) ? <Link className="inline-link" to="/gestion/servicios">Configurar servicios</Link> : 'Solicita a un administrador que habilite uno.'}</Alert>}
          {!priorities.length && <Alert>No hay políticas de atención activas. {isAdmin(session) ? <Link className="inline-link" to="/gestion/politicas">Configurar políticas</Link> : 'Contacta con administración.'}</Alert>}
          {!hasArea && <Alert>Tu cuenta necesita un área para crear tickets. {isAdmin(session) ? <Link className="inline-link" to={`/usuarios/${id}`}>Asignar área a mi cuenta</Link> : 'Solicita la asignación a un administrador.'}</Alert>}
          <label>Asunto<input name="asunto" placeholder="Describe el problema en una frase" required maxLength={200} /></label>
          <label>Servicio<select name="idServicio" defaultValue="" required><option value="" disabled>Selecciona un servicio</option>{services.map(service => <option value={service.idServicio} key={service.idServicio}>{service.nombre}</option>)}</select></label>
          <div className="form-row"><label>Tipo<select name="tipo" defaultValue="incidente"><option value="incidente">Incidente</option><option value="solicitud">Solicitud</option></select></label>
            <label>Prioridad<select name="prioridad" required defaultValue={priorities.includes('media') ? 'media' : priorities[0] ?? ''}>{!priorities.length && <option value="">Sin prioridades activas</option>}{Object.entries(priorityLabels).filter(([value]) => priorities.includes(value)).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label></div>
          <label>Descripción<textarea name="descripcion" placeholder="Indica qué estabas haciendo y qué sucedió." rows={6} required maxLength={10000} /></label>
          <div className="form-actions"><Link className="button secondary" to="/tickets">Cancelar</Link><button className="button primary" disabled={pending || !services.length || !priorities.length || !hasArea}>{pending ? 'Enviando…' : 'Crear ticket'}<ArrowRight size={18} /></button></div>
        </form>}
    </section>
  </>
}

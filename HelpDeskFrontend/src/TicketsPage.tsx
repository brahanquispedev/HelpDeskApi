import { ArrowDownRight, ChevronLeft, ChevronRight, Plus, Search, SlidersHorizontal } from 'lucide-react'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router'
import { can, isAdmin, message } from './api'
import type { Page, Ticket } from './api'
import { useSession } from './session'
import { Alert, Empty, Loading, Priority, Status, formatDate, statusLabels, ticketCode } from './ui'

const size = 10

export function TicketsPage() {
  const { call, session } = useSession()
  const [params, setParams] = useSearchParams()
  const search = params.get('buscar') ?? ''
  const status = params.get('estado') ?? ''
  const number = Number(params.get('pagina')) || 1
  const pageNumber = Number.isSafeInteger(number) ? Math.max(1, Math.min(1000000, number)) : 1
  const [attempt, setAttempt] = useState(0)
  const [result, setResult] = useState<Page<Ticket> | null>(null)
  const [error, setError] = useState('')
  const own = session?.roles.every(role => role === 'empleado')

  useEffect(() => {
    const abort = new AbortController()
    const query = new URLSearchParams({ pagina: String(pageNumber), tamano: String(size) })
    if (search) query.set('buscar', search)
    if (status) query.set('estado', status)
    setResult(null)
    setError('')
    call<Page<Ticket>>(`/api/Tickets?${query}`, { signal: abort.signal })
      .then(page => { if (!abort.signal.aborted) setResult(page) })
      .catch(error => { if (!abort.signal.aborted) setError(message(error)) })
    return () => abort.abort()
  }, [call, pageNumber, search, status, attempt])

  function update(nextSearch: string, nextStatus: string, page = 1) {
    const next = new URLSearchParams()
    if (nextSearch) next.set('buscar', nextSearch)
    if (nextStatus) next.set('estado', nextStatus)
    if (page > 1) next.set('pagina', String(page))
    setParams(next)
  }

  function filter(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    update(String(new FormData(event.currentTarget).get('buscar')).trim(), status)
  }

  return <>
    <div className="page-heading"><div><h1>{own ? 'Mis tickets' : 'Tickets'}</h1><p>Consulta el estado y el avance de las solicitudes de soporte.</p></div>
      {(can(session, 'tickets.crear') || isAdmin(session)) && <Link className="button primary" to="/tickets/nuevo"><Plus size={19} />Nuevo ticket</Link>}</div>
    <section className="panel ticket-panel" aria-label="Listado de tickets">
      <div className="table-tools">
        <form onSubmit={filter} className="search" key={search}><Search size={18} /><input name="buscar" aria-label="Buscar por asunto o descripción" placeholder="Buscar tickets…" defaultValue={search} maxLength={160} /><button className="button secondary compact">Buscar</button></form>
        <label className="filter"><SlidersHorizontal size={17} /><select aria-label="Filtrar por estado" value={status} onChange={event => update(search, event.target.value)}><option value="">Todos los estados</option>{Object.entries(statusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
        {(search || status) && <button className="text-button" type="button" onClick={() => update('', '')}>Limpiar</button>}
      </div>
      {error ? <div className="panel-message"><Alert>{error}</Alert><button className="button secondary" onClick={() => setAttempt(attempt + 1)}>Reintentar</button></div>
        : !result ? <Loading label="Cargando tickets…" />
        : <>
          {result.items.length ? <div className="table-scroll"><table className="ticket-table" role="table" aria-label="Solicitudes de soporte">
            <thead role="rowgroup"><tr role="row"><th role="columnheader" scope="col">Solicitud</th><th role="columnheader" scope="col">Estado</th><th role="columnheader" scope="col">Prioridad</th><th role="columnheader" scope="col">Creación</th></tr></thead>
            <tbody role="rowgroup">{result.items.map(ticket => <tr role="row" key={ticket.idTicket}>
              <td role="cell"><Link className="ticket-link" to={`/tickets/${ticket.idTicket}`}><span className="ticket-number">{ticketCode(ticket.numero)}</span><strong>{ticket.asunto}</strong><ArrowDownRight size={16} aria-hidden="true" /></Link></td>
              <td role="cell"><span className="mobile-label">Estado</span><Status value={ticket.estado} /></td><td role="cell"><span className="mobile-label">Prioridad</span><Priority value={ticket.prioridad} /></td><td role="cell" className="date-cell"><span className="mobile-label">Creación</span>{formatDate(ticket.fechaCreacion)}</td>
            </tr>)}</tbody>
          </table></div> : <Empty title="No hay tickets para mostrar">{search || status ? 'Prueba con otra búsqueda o cambia el filtro.' : 'Las solicitudes que puedas consultar aparecerán aquí.'}</Empty>}
          <div className="pagination"><span>{result.total} {result.total === 1 ? 'ticket' : 'tickets'}</span><div><button className="icon-button" aria-label="Página anterior" disabled={pageNumber === 1} onClick={() => update(search, status, pageNumber - 1)}><ChevronLeft size={19} /></button><span>Página {result.pagina}</span><button className="icon-button" aria-label="Página siguiente" disabled={result.pagina * result.tamano >= result.total} onClick={() => update(search, status, pageNumber + 1)}><ChevronRight size={19} /></button></div></div>
        </>}
    </section>
  </>
}

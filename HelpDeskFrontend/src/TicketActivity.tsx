import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { can, collect, isAdmin, message } from './api'
import type { Ticket } from './api'
import { useSession } from './session'
import { Alert, Loading, formatDate, statusLabels } from './ui'

type History = { idHistorial: string; accion: string; fecha: string; valorAnterior: string | null; valorNuevo: string | null; motivo: string | null }
type Comment = { idComentario: string; idAutor: string; mensaje: string; esInterno: boolean; fecha: string }
type Rating = { idUsuario: string; calificacion: number; comentario: string | null }
type Assignment = { idAsignacionTicket: string; nombreEquipo: string | null; nombreTecnico: string | null; fechaAsignacion: string; fechaFin: string | null }
const labels: Record<string, string> = { creado: 'Ticket creado', asignar: 'Ticket asignado', iniciar: 'Atención iniciada', resolver: 'Ticket resuelto', cerrar: 'Ticket cerrado', actualizado: 'Ticket actualizado' }

export function TicketActivity({ ticket }: { ticket: Ticket }) {
  const { call, session } = useSession()
  const [history, setHistory] = useState<History[] | null>(null)
  const [comments, setComments] = useState<Comment[] | null>(null)
  const [ratings, setRatings] = useState<Rating[] | null>(null)
  const [assignments, setAssignments] = useState<Assignment[] | null>(null)
  const [errors, setErrors] = useState<{ history?: string; comments?: string; rating?: string; assignments?: string }>({})
  const [retry, setRetry] = useState(0)
  const [saving, setSaving] = useState(false)
  const [submitError, setSubmitError] = useState('')
  const [commentEdit, setCommentEdit] = useState<Comment | null>(null)
  const terminal = ['cerrado', 'cancelado'].includes(ticket.estado)
  const rate = ticket.estado === 'cerrado' && ticket.idSolicitante === session?.idUsuario && (isAdmin(session) || can(session, 'encuestas.responder_propias'))
  const mayComment = !terminal && (isAdmin(session) || can(session, 'comentarios.responder'))
  const mayInternal = can(session, 'comentarios.internos')
  const { idTicket, version } = ticket

  useEffect(() => {
    const abort = new AbortController()
    const filter = `?idTicket=${encodeURIComponent(idTicket)}`
    setErrors({})
    collect<History>(call, `/api/HistorialTickets${filter}`, abort.signal)
      .then(result => { if (!abort.signal.aborted) setHistory(result) })
      .catch(error => { if (!abort.signal.aborted) setErrors(current => ({ ...current, history: message(error) })) })
    collect<Comment>(call, `/api/Comentarios${filter}`, abort.signal)
      .then(result => { if (!abort.signal.aborted) setComments(result) })
      .catch(error => { if (!abort.signal.aborted) setErrors(current => ({ ...current, comments: message(error) })) })
    collect<Assignment>(call, `/api/AsignacionesTickets${filter}`, abort.signal)
      .then(result => { if (!abort.signal.aborted) setAssignments(result) })
      .catch(error => { if (!abort.signal.aborted) setErrors(current => ({ ...current, assignments: message(error) })) })
    if (rate) collect<Rating>(call, `/api/Encuestas${filter}`, abort.signal)
      .then(result => { if (!abort.signal.aborted) setRatings(result) })
      .catch(error => { if (!abort.signal.aborted) setErrors(current => ({ ...current, rating: message(error) })) })
    return () => abort.abort()
  }, [call, idTicket, version, rate, retry])

  async function save(event: FormEvent<HTMLFormElement>, rating: boolean) {
    event.preventDefault()
    if (saving) return
    const element = event.currentTarget
    const form = new FormData(element)
    const text = String(form.get('mensaje') ?? '').trim()
    if (!rating && !text) { setSubmitError('Escribe un comentario.'); return }
    setSaving(true)
    setSubmitError('')
    try {
      if (rating) {
        const result = await call<Rating>('/api/Encuestas', { method: 'POST', body: { idTicket, calificacion: Number(form.get('calificacion')), comentario: text || null } })
        setRatings(current => [...(current ?? []), result])
      } else {
        const path = commentEdit ? `/api/Comentarios/${commentEdit.idComentario}` : '/api/Comentarios'
        const result = await call<Comment>(path, { method: commentEdit ? 'PUT' : 'POST', body: { idTicket, mensaje: text, esInterno: mayInternal && form.has('esInterno') } })
        setComments(current => [...(current ?? []).filter(item => item.idComentario !== result.idComentario), result])
        setCommentEdit(null)
      }
      element.reset()
    } catch (error) { setSubmitError(message(error)) }
    finally { setSaving(false) }
  }

  const existingRating = ratings?.find(rating => rating.idUsuario === session?.idUsuario)
  const retryButton = <button className="button secondary compact" onClick={() => setRetry(current => current + 1)}>Reintentar</button>
  return <div className="activity-grid">
    <section className="panel detail-panel"><h2>Comentarios</h2>
      {errors.comments ? <><Alert>{errors.comments}</Alert>{retryButton}</> : !comments ? <Loading /> : !comments.length ? <p className="ticket-description">Todavía no hay comentarios.</p> : <ul className="activity-list">{[...comments].sort((a, b) => Date.parse(a.fecha) - Date.parse(b.fecha)).map(comment => <li key={comment.idComentario}>
        <p className="ticket-number">{formatDate(comment.fecha)}{comment.idAutor === session?.idUsuario && ' · Tú'}{comment.esInterno && ' · Nota interna'}</p><p className="ticket-description">{comment.mensaje}</p>
        {isAdmin(session) && !terminal && (!comment.esInterno || mayInternal) && <button className="button secondary compact" disabled={saving} onClick={() => { setSubmitError(''); setCommentEdit(comment) }}>Editar comentario</button>}
      </li>)}</ul>}
      {mayComment && <form className="form" key={commentEdit?.idComentario ?? 'new'} onSubmit={event => void save(event, false)}>
        {submitError && !rate && <Alert>{submitError}</Alert>}
        <label>{commentEdit ? 'Editar comentario' : 'Nuevo comentario'}<textarea name="mensaje" required rows={3} maxLength={10000} defaultValue={commentEdit?.mensaje} /></label>
        {mayInternal && <label className="account-toggle"><input type="checkbox" name="esInterno" defaultChecked={commentEdit?.esInterno} />Nota interna</label>}
        <div className="form-actions">{commentEdit && <button type="button" className="button secondary" disabled={saving} onClick={() => { setSubmitError(''); setCommentEdit(null) }}>Cancelar</button>}<button className="button primary" disabled={saving}>{commentEdit ? 'Guardar comentario' : 'Publicar comentario'}</button></div>
      </form>}
    </section>
    <section className="panel detail-panel"><h2>Historial</h2>
      {errors.history ? <><Alert>{errors.history}</Alert>{retryButton}</> : !history ? <Loading /> : !history.length ? <p className="ticket-description">Todavía no hay movimientos.</p> : <ol className="activity-list">{[...history].sort((a, b) => Date.parse(b.fecha) - Date.parse(a.fecha)).map(item => <li key={item.idHistorial}>
        <h3>{labels[item.accion] ?? item.accion.replaceAll('_', ' ')}</h3><p className="ticket-number">{formatDate(item.fecha)}</p>
        {item.valorNuevo && <p className="ticket-description">{item.valorAnterior && `${statusLabels[item.valorAnterior] ?? item.valorAnterior} → `}{statusLabels[item.valorNuevo] ?? item.valorNuevo}</p>}
        {item.motivo && <p className="ticket-description">{item.motivo}</p>}
      </li>)}</ol>}
    </section>
    <section className="panel detail-panel"><h2>Asignaciones</h2>
      {errors.assignments ? <><Alert>{errors.assignments}</Alert>{retryButton}</> : !assignments ? <Loading /> : !assignments.length ? <p className="ticket-description">Todavía no hay asignaciones.</p> : <ul className="activity-list">{[...assignments].sort((a, b) => Date.parse(b.fechaAsignacion) - Date.parse(a.fechaAsignacion)).map(item => <li key={item.idAsignacionTicket}>
        <h3>{item.nombreEquipo ?? 'Equipo de soporte'}</h3>
        <p className="ticket-description">{item.nombreTecnico ?? 'Pendiente de asignar técnico'}{!item.fechaFin && ' · Vigente'}</p>
        <p className="ticket-number">{formatDate(item.fechaAsignacion)}{item.fechaFin && ` → ${formatDate(item.fechaFin)}`}</p>
      </li>)}</ul>}
    </section>
    {rate && <section className="panel detail-panel"><h2>Evaluar atención</h2>{errors.rating ? <><Alert>{errors.rating}</Alert>{retryButton}</> : !ratings ? <Loading /> : existingRating ? <><p className="ticket-description">Calificación: {existingRating.calificacion} de 5.</p>{existingRating.comentario && <p className="ticket-description">{existingRating.comentario}</p>}</> : <form className="form" onSubmit={event => void save(event, true)}>{submitError && <Alert>{submitError}</Alert>}
      <label>Calificación<select name="calificacion" defaultValue="" required><option value="" disabled>Selecciona de 1 a 5</option>{[1, 2, 3, 4, 5].map(value => <option key={value}>{value}</option>)}</select></label><label>Comentario opcional<textarea name="mensaje" rows={3} maxLength={10000} /></label><div className="form-actions"><button className="button primary" disabled={saving}>Enviar evaluación</button></div>
    </form>}</section>}
  </div>
}

import { Plus } from 'lucide-react'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, Navigate, useParams } from 'react-router'
import { can, collect, message } from './api'
import { groups, mayRead, mayWrite } from './catalog'
import type { Catalog, Field, Row } from './catalog'
import { useSession } from './session'
import { Alert, Empty, Loading, formatDate } from './ui'

type Editor = { row?: Row; action?: NonNullable<Catalog['actions']>[number]; delete?: boolean }
type References = Record<string, Row[]>

export function Setup() {
  const { session } = useSession()
  if (!groups.some(group => group.entries.some(entry => mayRead(session, entry)))) return <Navigate to="/tickets" replace />
  return <><div className="page-heading"><div><h1>Gestión</h1><p>Configura y administra la mesa de ayuda.</p></div></div>
    <div className="management-grid">{groups.map(group => {
      const entries = group.entries.filter(entry => mayRead(session, entry))
      if (!entries.length) return null
      return <section className="panel detail-panel" key={group.title}><h2>{group.title}</h2><div className="management-links">{entries.map(entry => <Link className="button secondary" to={`/gestion/${entry.slug}`} key={entry.slug}>{entry.title}</Link>)}</div></section>
    })}</div>
  </>
}

export function CatalogRoute() {
  const { section } = useParams()
  const { session } = useSession()
  const config = groups.flatMap(group => group.entries).find(entry => entry.slug === section)
  return config && mayRead(session, config) ? <CatalogPage key={config.slug} config={config} /> : <Navigate to="/tickets" replace />
}

function cell(field: Field, row: Row, references: References) {
  const value = row[field.name]
  if (value === null || value === undefined || value === '') return '—'
  if (typeof value === 'boolean') return value ? 'Sí' : 'No'
  if (field.kind === 'datetime') return formatDate(String(value))
  const ref = field.ref
  if (ref) return String(references[ref.route]?.find(option => option[ref.id] === value)?.[ref.label] ?? 'Referencia no disponible')
  if (field.name === 'estado') return String(value).replaceAll('_', ' ')
  return field.choices?.[String(value)] ?? String(value)
}

function formValue(field: Field, form: FormData) {
  if (field.kind === 'boolean') return form.has(field.name)
  const value = String(form.get(field.name) ?? '').trim()
  if (!value) {
    if (field.required) throw new Error(`Completa el campo «${field.label}».`)
    return null
  }
  if (field.kind === 'number') return Number(value)
  if (field.kind === 'datetime') return new Date(value).toISOString()
  return value
}

function fieldInput(field: Field, editor: Editor, references: References) {
  const value = editor.action ? field.initial : editor.row?.[field.name] ?? field.initial
  const attributes = { name: field.name, required: field.required }
  if (field.kind === 'boolean') return <input type="checkbox" name={field.name} defaultChecked={value === true} />
  if (field.kind === 'long') return <textarea {...attributes} rows={3} maxLength={field.max} defaultValue={String(value ?? '')} />
  if (field.kind === 'number') return <input {...attributes} type="number" min={1} step={1} defaultValue={String(value ?? '')} />
  if (field.kind === 'datetime') {
    const date = value ? new Date(String(value)) : null
    if (date) date.setMinutes(date.getMinutes() - date.getTimezoneOffset())
    return <input {...attributes} type="datetime-local" defaultValue={date?.toISOString().slice(0, 16) ?? ''} />
  }
  if (field.kind === 'select') {
    const ref = field.ref
    const options = ref ? (references[ref.route] ?? [])
      .filter(option => !ref.enabled || option[ref.enabled] === true || option[ref.id] === value)
      .map(option => [String(option[ref.id]), String(option[ref.label])]) : Object.entries(field.choices ?? {})
    const selected = String(value ?? '')
    return <select {...attributes} defaultValue={selected}><option value="">{field.required ? `Selecciona ${field.label.toLowerCase()}` : 'Ninguno'}</option>
      {selected && !options.some(([id]) => id === selected) && <option value={selected}>Selección actual</option>}
      {options.map(([id, label]) => <option value={id} key={id}>{label}</option>)}</select>
  }
  return <input {...attributes} maxLength={field.max} defaultValue={String(value ?? '')} />
}

function CatalogPage({ config }: { config: Catalog }) {
  const { call, session } = useSession()
  const writable = mayWrite(session, config)
  const [rows, setRows] = useState<Row[] | null>(null)
  const [refs, setRefs] = useState<References>({})
  const [referenceErrors, setReferenceErrors] = useState<string[]>([])
  const [loadError, setLoadError] = useState('')
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [pending, setPending] = useState(false)
  const [attempt, setAttempt] = useState(0)
  const [editor, setEditor] = useState<Editor | null>(null)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const route = `/api/${config.route}`
  const fields = editor?.action?.fields ?? config.fields?.filter(field => (editor?.row ? !field.createOnly : !field.editOnly)
    && !(config.slug === 'adjuntos' && field.name === 'esInterno' && !can(session, 'comentarios.internos'))) ?? []

  useEffect(() => {
    const abort = new AbortController()
    const refs = [...new Set([...config.columns, ...(writable ? config.fields ?? [] : [])]
      .flatMap(field => field.ref ? [field.ref.route] : []))]
    setLoadError('')
    collect<Row>(call, route, abort.signal).then(result => { if (!abort.signal.aborted) setRows(result) })
      .catch(error => { if (!abort.signal.aborted) setLoadError(message(error)) })
    Promise.allSettled(refs.map(path => collect<Row>(call, path, abort.signal))).then(results => {
      if (abort.signal.aborted) return
      setRefs(Object.fromEntries(results.flatMap((result, index) => result.status === 'fulfilled' ? [[refs[index], result.value]] : [])))
      setReferenceErrors(refs.filter((_, index) => results[index].status === 'rejected'))
    })
    return () => abort.abort()
  }, [call, route, config, writable, attempt])

  function open(next: Editor) { setEditor(next); setError(''); setNotice('') }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!editor || pending) return
    setPending(true)
    setError('')
    try {
      const data = new FormData(event.currentTarget)
      const path = editor.row ? `${route}/${config.ids.map(id => encodeURIComponent(String(editor.row?.[id]))).join('/')}` : route
      if (editor.delete) await call(path, { method: 'DELETE' })
      else {
        const body = Object.fromEntries(fields.map(field => [field.name, formValue(field, data)]))
        if (config.slug === 'articulos-tickets' && session) body.idUsuario = session.idUsuario
        if (config.slug === 'adjuntos' && editor.row) {
          body.idComentario = editor.row.idComentario
          if (!can(session, 'comentarios.internos')) body.esInterno = editor.row.esInterno
        }
        if ((config.slug === 'aprobaciones') && Boolean(body.idTicket) === Boolean(body.idCambio)) throw new Error('Selecciona un ticket o un cambio.')
        if (config.slug === 'politicas' && Number(body.minutosResolucion) < Number(body.minutosPrimeraRespuesta)) throw new Error('El plazo de resolución debe ser igual o mayor al de primera respuesta.')
        await call(editor.action ? `${path}/${editor.action.suffix}` : path, { method: editor.action || !editor.row ? 'POST' : 'PUT', body })
      }
      setEditor(null)
      setNotice('Cambios guardados.')
      setAttempt(current => current + 1)
    } catch (error) { setError(message(error)) }
    finally { setPending(false) }
  }

  const searchText = search.toLocaleLowerCase('es')
  const filtered = rows?.filter(row => config.columns.some(field => cell(field, row, refs).toLocaleLowerCase('es').includes(searchText))) ?? []
  const pages = Math.max(1, Math.ceil(filtered.length / 15))
  const current = Math.min(page, pages)
  const pageRows = filtered.slice((current - 1) * 15, current * 15)
  const approval = config.slug === 'aprobaciones' && can(session, 'aprobaciones.decidir')
  const approvalActions = [{ label: 'Aprobar', suffix: 'aprobar', fields: [{ name: 'motivo', label: 'Motivo', required: true }] }, { label: 'Rechazar', suffix: 'rechazar', fields: [{ name: 'motivo', label: 'Motivo', required: true }] }]
  const editing = Boolean(editor?.row)
  const missing = fields.some(field => field.ref && (!refs[field.ref.route] || (field.required && !refs[field.ref.route].some(row => {
    const ref = field.ref!
    return !ref.enabled || row[ref.enabled] === true || row[ref.id] === editor?.row?.[field.name]
  }))))

  return <><Link className="back" to="/gestion">Volver a gestión</Link><div className="page-heading"><div><h1>{config.title}</h1></div>
    {writable && config.fields && <button className="button primary" onClick={() => open({})} disabled={pending}><Plus size={17} />Agregar</button>}</div>
    {notice && <p className="success reload" role="status">{notice}</p>}
    {editor && <section className="panel action-panel catalog-editor"><h2>{editor.delete ? config.remove : editor.action?.label ?? (editing ? 'Editar registro' : 'Nuevo registro')}</h2>
      {error && <Alert>{error}</Alert>}
      {!editor.delete && missing && <Alert>{referenceErrors.length ? 'No se pudo cargar una opción relacionada. Reintenta la carga.' : 'Faltan opciones relacionadas. Crea o activa los registros que requiere el formulario.'}</Alert>}
      {!editor.delete && missing && <button className="button secondary reload" onClick={() => setAttempt(value => value + 1)}>Recargar opciones</button>}
      <form className="form" key={`${editor.row ? config.ids.map(id => editor.row?.[id]).join(':') : 'new'}-${editor.action?.label ?? ''}-${attempt}`} onSubmit={save}>
        {editor.delete ? <p>Confirma la operación sobre este registro.</p> : fields.map(field => <label key={field.name} className={field.kind === 'boolean' ? 'account-toggle' : undefined}>{field.kind === 'boolean' ? <>{fieldInput(field, editor, refs)}{field.label}</> : <>{field.label}{fieldInput(field, editor, refs)}</>}</label>)}
        <div className="form-actions"><button className="button secondary" type="button" disabled={pending} onClick={() => setEditor(null)}>Cancelar</button><button className="button primary" disabled={pending || (!editor.delete && missing)}>{pending ? 'Guardando…' : editor.delete ? 'Confirmar' : editor.action?.label ?? 'Guardar'}</button></div>
      </form>
    </section>}
    <section className="panel"><div className="table-tools"><input className="catalog-search" placeholder={`Buscar en ${config.title.toLocaleLowerCase('es')}…`} aria-label={`Buscar en ${config.title}`} value={search} onChange={event => { setSearch(event.target.value); setPage(1) }} /></div>
      {loadError ? <div className="panel-message"><Alert>{loadError}</Alert><button className="button secondary" onClick={() => setAttempt(value => value + 1)}>Reintentar</button></div> : !rows ? <Loading /> : !filtered.length ? <Empty title="No hay registros">{search ? 'Prueba con otra búsqueda.' : 'Los registros aparecerán aquí.'}</Empty> : <>
        <div className="table-scroll"><table><thead><tr>{config.columns.map(field => <th key={field.name} scope="col">{field.label}</th>)}{(writable || approval) && <th scope="col">Acciones</th>}</tr></thead>
          <tbody>{pageRows.map(row => <tr key={config.ids.map(id => row[id]).join(':')}>
            {config.columns.map(field => <td key={field.name} className="catalog-cell">{cell(field, row, refs)}</td>)}
            {(writable || approval) && <td><div className="inline-actions">
              {writable && config.update && <button className="button secondary compact" disabled={pending} onClick={() => open({ row })}>Editar</button>}
              {writable && config.remove && (config.remove !== 'Desactivar' || row.activo) && <button className="button secondary compact" disabled={pending} onClick={() => open({ row, delete: true })}>{config.remove}</button>}
              {writable && config.actions?.filter(action => !action.when || action.when(row)).map(action => <button className="button secondary compact" key={action.label} disabled={pending} onClick={() => open({ row, action })}>{action.label}</button>)}
              {approval && row.estado === 'pendiente' && row.idAprobador === session?.idUsuario && approvalActions.map(action => <button className="button secondary compact" key={action.suffix} disabled={pending} onClick={() => open({ row, action })}>{action.label}</button>)}
            </div></td>}
          </tr>)}</tbody></table></div>
        <div className="pagination"><span>{filtered.length} registros</span><div><button className="button secondary compact" disabled={current <= 1} onClick={() => setPage(current - 1)}>Anterior</button><span>Página {current} de {pages}</span><button className="button secondary compact" disabled={current >= pages} onClick={() => setPage(current + 1)}>Siguiente</button></div></div>
      </>}</section>
  </>
}

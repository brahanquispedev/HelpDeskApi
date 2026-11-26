import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { message } from './api'
import type { Page, RequestOptions } from './api'
import { useSession } from './session'
import { Alert, Empty, Loading } from './ui'

type User = { idUsuario: string; idArea: string | null; nombres: string; apellidos: string; nombreUsuario: string; correo: string; telefono: string | null; activo: boolean }
type Area = { idArea: string; nombre: string; activo: boolean }
type Role = { idRol: string; nombre: string }
type Assignment = { idUsuario: string; idRol: string }
type Call = <T>(path: string, options?: RequestOptions) => Promise<T>

function credentials(form: FormData) {
  return {
    nombres: String(form.get('nombres')).trim(), apellidos: String(form.get('apellidos')).trim(),
    nombreUsuario: String(form.get('nombreUsuario')).trim(), correo: String(form.get('correo')).trim(),
    clave: String(form.get('clave') ?? ''),
  }
}

function IdentityFields({ user }: { user?: User }) {
  return <>
    <div className="form-row"><label>Nombres<input name="nombres" defaultValue={user?.nombres} required maxLength={100} /></label>
      <label>Apellidos<input name="apellidos" defaultValue={user?.apellidos} required maxLength={150} /></label></div>
    <label>Nombre de usuario<input name="nombreUsuario" defaultValue={user?.nombreUsuario} autoComplete="username" required maxLength={100} /></label>
    <label>Correo<input name="correo" type="email" defaultValue={user?.correo} autoComplete="email" required maxLength={254} /></label>
  </>
}

async function allPages<T>(call: Call, route: string, signal: AbortSignal) {
  const items: T[] = []
  for (let pagina = 1; ; pagina++) {
    const page = await call<Page<T>>(`${route}?pagina=${pagina}&tamano=100`, { signal })
    items.push(...page.items)
    if (page.items.length === 0 || pagina * page.tamano >= page.total) return items
  }
}

export function Users() {
  const { call } = useSession()
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<Page<User> | null>(null)
  const [error, setError] = useState('')
  const [retry, setRetry] = useState(0)
  useEffect(() => {
    const abort = new AbortController()
    setData(null)
    setError('')
    const query = new URLSearchParams({ pagina: String(page), tamano: '20', buscar: search })
    call<Page<User>>(`/api/Usuarios?${query}`, { signal: abort.signal })
      .then(result => { if (!abort.signal.aborted) setData(result) })
      .catch(error => { if (!abort.signal.aborted) setError(message(error)) })
    return () => abort.abort()
  }, [call, page, search, retry])
  return <><div className="page-heading"><div><h1>Usuarios</h1><p>Administra cuentas, áreas y roles.</p></div><Link className="button primary" to="/usuarios/nuevo">Nuevo usuario</Link></div>
    <section className="panel"><div className="table-tools"><form className="search" onSubmit={event => {
      event.preventDefault()
      setSearch(String(new FormData(event.currentTarget).get('buscar')).trim())
      setPage(1)
    }}><input name="buscar" placeholder="Buscar usuarios…" aria-label="Buscar usuarios" /><button className="button secondary">Buscar</button></form></div>
      {error ? <div className="panel-message"><Alert>{error}</Alert><button className="button secondary" onClick={() => setRetry(retry + 1)}>Reintentar</button></div> : !data ? <Loading /> : <>
        {data.items.length ? <div className="table-scroll"><table><thead><tr><th scope="col">Nombre</th><th scope="col">Correo</th><th scope="col">Estado</th><th scope="col">Área</th></tr></thead><tbody>{data.items.map(user => <tr key={user.idUsuario}>
          <td><Link className="ticket-link" to={`/usuarios/${user.idUsuario}`}><strong>{user.nombres} {user.apellidos}</strong><span className="ticket-number">{user.nombreUsuario}</span></Link></td><td>{user.correo}</td><td>{user.activo ? 'Activo' : 'Inactivo'}</td><td>{user.idArea ? 'Asignada' : 'Sin área'}</td>
        </tr>)}</tbody></table></div> : <Empty title="No encontramos usuarios">Prueba con otra búsqueda.</Empty>}
        <div className="pagination"><span>{data.total} usuarios</span><div><button className="button secondary compact" disabled={page === 1} onClick={() => setPage(page - 1)}>Anterior</button><span>Página {page}</span><button className="button secondary compact" disabled={page * data.tamano >= data.total} onClick={() => setPage(page + 1)}>Siguiente</button></div></div>
      </>}
    </section></>
}

export function UserRoute() {
  const { id } = useParams()
  return <UserEditor key={id ?? 'nuevo'} id={id} />
}

function UserEditor({ id }: { id?: string }) {
  const { call } = useSession()
  const navigate = useNavigate()
  const [record, setRecord] = useState<User>()
  const [areas, setAreas] = useState<Area[] | null>(null)
  const [roles, setRoles] = useState<Role[]>([])
  const [assigned, setAssigned] = useState<Assignment[]>([])
  const [loadError, setLoadError] = useState('')
  const [retry, setRetry] = useState(0)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [pending, setPending] = useState(false)
  const [roleId, setRoleId] = useState('')

  useEffect(() => {
    const abort = new AbortController()
    const options = { signal: abort.signal }
    setLoadError('')
    Promise.all([
      call<Area[]>('/api/Areas', options),
      id ? call<User>(`/api/Usuarios/${id}`, options) : undefined,
      id ? allPages<Role>(call, '/api/Roles', abort.signal) : [],
      id ? allPages<Assignment>(call, '/api/UsuariosRoles', abort.signal) : [],
    ]).then(([areas, user, roles, assignments]) => {
      if (abort.signal.aborted) return
      setAreas(areas)
      setRecord(user)
      setRoles(roles)
      setAssigned(assignments.filter(assignment => assignment.idUsuario === id))
    }).catch(error => { if (!abort.signal.aborted) setLoadError(message(error)) })
    return () => abort.abort()
  }, [call, id, retry])

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const { clave, ...identity } = credentials(form)
    const body = {
      ...identity, idArea: form.get('idArea') || null, telefono: String(form.get('telefono')).trim() || null,
      activo: form.has('activo'), ...(clave ? { clave } : {}),
    }
    setPending(true)
    setError('')
    setNotice('')
    try {
      const user = await call<User>(id ? `/api/Usuarios/${id}` : '/api/Usuarios', { method: id ? 'PUT' : 'POST', body })
      if (!id) { navigate(`/usuarios/${user.idUsuario}`, { replace: true }); return }
      setRecord(user)
      setNotice('Datos del usuario guardados.')
    } catch (error) { setError(message(error)) }
    finally { setPending(false) }
  }

  async function updateRole(action: 'add' | 'remove', selected: string) {
    if (!id || !selected || pending) return
    setPending(true)
    setError('')
    setNotice('')
    try {
      if (action === 'add') {
        const assignment = await call<Assignment>('/api/UsuariosRoles', { method: 'POST', body: { idUsuario: id, idRol: selected } })
        setAssigned(current => [...current, assignment])
        setRoleId('')
      } else {
        await call(`/api/UsuariosRoles/${id}/${selected}`, { method: 'DELETE' })
        setAssigned(current => current.filter(assignment => assignment.idRol !== selected))
      }
      setNotice('Roles del usuario actualizados.')
    } catch (error) { setError(message(error)) }
    finally { setPending(false) }
  }

  return <><Link className="back" to="/usuarios">Volver a usuarios</Link><div className="page-heading"><div><h1>{id ? 'Editar usuario' : 'Nuevo usuario'}</h1><p>{id ? record?.nombreUsuario : 'Después de guardar la cuenta podrás asignarle sus roles.'}</p></div></div>
    {loadError ? <div className="panel panel-message"><Alert>{loadError}</Alert><button className="button secondary" onClick={() => setRetry(retry + 1)}>Reintentar</button></div> : !areas || (id && !record) ? <Loading /> : <>
      {error && <Alert>{error}</Alert>}{notice && <p className="success reload" role="status">{notice}</p>}
      <section className="panel form-panel"><form className="form" onSubmit={save}>
        <IdentityFields user={record} />
        <div className="form-row"><label>Área<select name="idArea" defaultValue={record?.idArea ?? ''}><option value="">Sin área</option>{areas.filter(area => area.activo || area.idArea === record?.idArea).map(area => <option key={area.idArea} value={area.idArea}>{area.nombre}{area.activo ? '' : ' (inactiva)'}</option>)}</select></label><label>Teléfono<input type="tel" name="telefono" maxLength={30} defaultValue={record?.telefono ?? ''} /></label></div>
        <label>{id ? 'Nueva contraseña (opcional)' : 'Contraseña inicial'}<input name="clave" type="password" autoComplete="new-password" required={!id} minLength={10} maxLength={128} /></label>
        <label className="account-toggle"><input type="checkbox" name="activo" defaultChecked={record?.activo ?? true} />Cuenta activa</label>
        <div className="form-actions"><button className="button primary" disabled={pending}>{pending ? 'Guardando…' : id ? 'Guardar cambios' : 'Crear usuario'}</button></div>
      </form></section>
      {id && <section className="panel action-panel"><h2>Roles asignados</h2>
        {assigned.length ? <ul className="role-list">{assigned.map(({ idRol }) => {
          const name = roles.find(role => role.idRol === idRol)?.nombre ?? idRol
          return <li key={idRol}><strong>{name}</strong><button className="button secondary compact" disabled={pending} aria-label={`Quitar rol ${name}`} onClick={() => void updateRole('remove', idRol)}>Quitar</button></li>
        })}</ul> : <p className="intro">Esta cuenta todavía no tiene roles. Asigna uno para habilitar sus permisos.</p>}
        <form className="form" onSubmit={event => { event.preventDefault(); void updateRole('add', roleId) }}>
          <label>Agregar rol<select value={roleId} onChange={event => setRoleId(event.target.value)} required><option value="">Selecciona un rol</option>{roles.filter(role => !assigned.some(item => item.idRol === role.idRol)).map(role => <option key={role.idRol} value={role.idRol}>{role.nombre}</option>)}</select></label>
          <div className="form-actions"><button className="button primary" disabled={pending || !roleId}>Asignar rol</button></div>
        </form>
      </section>}
    </>}
  </>
}

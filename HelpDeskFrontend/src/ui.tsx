import { CircleAlert, Headset, LoaderCircle, Ticket as TicketIcon } from 'lucide-react'
import type { ReactNode } from 'react'

export const statusLabels: Record<string, string> = {
  nuevo: 'Nuevo', asignado: 'Asignado', en_atencion: 'En atención', en_espera: 'En espera',
  resuelto: 'Resuelto', cerrado: 'Cerrado', cancelado: 'Cancelado',
}

export const priorityLabels: Record<string, string> = {
  baja: 'Baja', media: 'Media', alta: 'Alta', critica: 'Crítica',
}

export const formatDate = (value: string) => new Intl.DateTimeFormat('es-PE', {
  dateStyle: 'medium', timeStyle: 'short', timeZone: 'America/Lima',
}).format(new Date(value))

export const ticketCode = (number: number) => `#${String(number).padStart(4, '0')}`

export function Brand() {
  return <div className="brand"><span className="brand-mark"><Headset size={21} /></span><span>HelpDesk</span></div>
}

export function Status({ value }: { value: string }) {
  return <span className={`status status-${value}`}><i />{statusLabels[value] ?? value}</span>
}

export function Priority({ value }: { value: string }) {
  return <span className={`priority priority-${value}`}><i />{priorityLabels[value] ?? value}</span>
}

export function Alert({ children }: { children: ReactNode }) {
  return <div className="alert" role="alert"><CircleAlert size={18} /><span>{children}</span></div>
}

export function Loading({ label = 'Cargando…' }: { label?: string }) {
  return <div className="loading" role="status"><LoaderCircle className="spin" size={20} />{label}</div>
}

export function Empty({ title, children }: { title: string; children: ReactNode }) {
  return <div className="empty"><span className="empty-mark"><TicketIcon size={24} /></span><h2>{title}</h2><p>{children}</p></div>
}

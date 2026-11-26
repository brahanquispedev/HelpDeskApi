export const noticeEvents = {
  ticket_creado: 'Ticket nuevo para tu equipo',
  ticket_asignado: 'Asignación de ticket',
  ticket_en_atencion: 'Inicio de atención',
  ticket_resuelto: 'Resolución de ticket',
  ticket_cerrado: 'Cierre de ticket',
  ticket_comentario: 'Comentario público nuevo',
}

export function noticeEventLabel(type: string) {
  return noticeEvents[type as keyof typeof noticeEvents] ?? type
}

const labels:Record<string,string>={'ticket.created':'Ticket created','ticket.updated':'Ticket updated','ticket.cancelled':'Ticket cancelled','ticket.assigned':'Ticket assigned','ticket.status_changed':'Status changed','ticket.comment_added':'Comment added','ticket.internal_comment_added':'Internal note added','ticket.attachment_uploaded':'Attachment uploaded','ticket.attachment_deleted':'Attachment deleted'}

export function friendlyActivityLabel(value:string){
  return labels[value]??value.replace(/[._]/g,' ').replace(/\b\w/g,letter=>letter.toUpperCase())
}

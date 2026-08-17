import { useNavigate } from 'react-router-dom'
import { createTicketAsync } from '../api/tickets'
import { TicketForm } from '../components/TicketForm'

export function CreateTicketPage() {
  const navigate=useNavigate()
  return <section>
    <div className="page-heading"><div><h1>Create ticket</h1><p>Describe the issue and set its initial priority.</p></div></div>
    <TicketForm cancelTo="/app/tickets" onSubmit={async request=>{const ticket=await createTicketAsync(request);navigate(`/app/tickets/${ticket.id}`)}}/>
  </section>
}

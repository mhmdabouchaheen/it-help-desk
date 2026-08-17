import { useNavigate,useParams } from 'react-router-dom'
import { updateTicketAsync } from '../api/tickets'
import { useTicketDetail } from '../auth/useTicketDetail'
import { TicketForm } from '../components/TicketForm'
import { LoadingIndicator } from '../components/Feedback'
import { isGuid } from '../utils/tickets'

export function EditTicketPage(){const{id}=useParams();const navigate=useNavigate();if(!isGuid(id))return<h1>Invalid ticket</h1>;return <Loaded id={id!} navigate={navigate}/>}
function Loaded({id,navigate}:{id:string;navigate:(to:string)=>void}){const{ticket,loading,error}=useTicketDetail(id);if(loading)return<LoadingIndicator/>;if(error||!ticket)return<h1>{error??'Ticket not found.'}</h1>;return<section><div className="page-heading"><div><h1>Edit {ticket.ticketNumber}</h1><p>Update the request details without changing its workflow history.</p></div></div><TicketForm initial={{title:ticket.title,description:ticket.description,categoryId:ticket.categoryId,priorityId:ticket.priorityId}} cancelTo={`/app/tickets/${id}`} onSubmit={async request=>{await updateTicketAsync(id,request);navigate(`/app/tickets/${id}`)}}/></section>}

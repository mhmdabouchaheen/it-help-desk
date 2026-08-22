import {lazy,Suspense} from 'react'
import {Navigate,Route,Routes} from 'react-router-dom'
import './dashboard.css'
import {AppLayout} from './layouts/AppLayout'
import {LoginPage} from './pages/LoginPage'
import {NotFoundPage} from './pages/NotFoundPage'
import {RegisterPage} from './pages/RegisterPage'
import {ForgotPasswordPage} from './pages/ForgotPasswordPage'
import {ResetPasswordPage} from './pages/ResetPasswordPage'
import {ProtectedRoute,PublicOnlyRoute,SupportOnlyRoute} from './routes/RouteGuards'
import './App.css'
import './auth.css'
import './audit.css'
import './styles/tokens.css'
import './styles/polish.css'
const ActivityLogPage=lazy(()=>import('./pages/ActivityLogPage').then(x=>({default:x.ActivityLogPage})))
const ReportsPage=lazy(()=>import('./pages/ReportsPage').then(x=>({default:x.ReportsPage})))
const ProfilePage=lazy(()=>import('./pages/ProfilePage').then(x=>({default:x.ProfilePage})))
const HomePage=lazy(()=>import('./pages/HomePage').then(x=>({default:x.HomePage})));const TicketListPage=lazy(()=>import('./pages/TicketListPage').then(x=>({default:x.TicketListPage})));const CreateTicketPage=lazy(()=>import('./pages/CreateTicketPage').then(x=>({default:x.CreateTicketPage})));const TicketDetailPage=lazy(()=>import('./pages/TicketDetailPage').then(x=>({default:x.TicketDetailPage})));const EditTicketPage=lazy(()=>import('./pages/EditTicketPage').then(x=>({default:x.EditTicketPage})));const NotificationsPage=lazy(()=>import('./pages/NotificationsPage').then(x=>({default:x.NotificationsPage})));const route=(page:React.ReactNode)=><Suspense fallback={<div className="route-loading" role="status" aria-live="polite"><span className="spinner"/>Loading pageâ€¦</div>}>{page}</Suspense>
export default function App(){return <Routes><Route element={<PublicOnlyRoute/>}><Route path="/login" element={<LoginPage/>}/><Route path="/register" element={<RegisterPage/>}/><Route path="/forgot-password" element={<ForgotPasswordPage/>}/><Route path="/reset-password" element={<ResetPasswordPage/>}/></Route><Route element={<ProtectedRoute/>}><Route path="/app" element={<AppLayout/>}><Route index element={<Navigate to="home" replace/>}/><Route path="home" element={route(<HomePage/>)}/><Route path="profile" element={route(<ProfilePage/>)}/><Route path="tickets" element={route(<TicketListPage/>)}/><Route path="tickets/new" element={route(<CreateTicketPage/>)}/><Route path="tickets/:id" element={route(<TicketDetailPage/>)}/><Route path="tickets/:id/edit" element={route(<EditTicketPage/>)}/><Route path="notifications" element={route(<NotificationsPage/>)}/><Route element={<SupportOnlyRoute/>}><Route path="activity" element={route(<ActivityLogPage/>)}/><Route path="reports" element={route(<ReportsPage/>)}/></Route></Route></Route><Route path="/" element={<Navigate to="/app/home" replace/>}/><Route path="*" element={<NotFoundPage/>}/></Routes>}

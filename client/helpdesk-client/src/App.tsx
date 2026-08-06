import { Navigate, Route, Routes } from 'react-router-dom'
import './dashboard.css'
import { AppLayout } from './layouts/AppLayout'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { RegisterPage } from './pages/RegisterPage'
import {TicketListPage} from './pages/TicketListPage'
import {CreateTicketPage} from './pages/CreateTicketPage'
import {TicketDetailPage} from './pages/TicketDetailPage'
import {EditTicketPage} from './pages/EditTicketPage'
import {NotificationsPage} from './pages/NotificationsPage'
import { ProtectedRoute, PublicOnlyRoute } from './routes/RouteGuards'
import './App.css'
export default function App() { return <Routes>
  <Route element={<PublicOnlyRoute />}><Route path="/login" element={<LoginPage />} /><Route path="/register" element={<RegisterPage />} /></Route>
  <Route element={<ProtectedRoute />}><Route path="/app" element={<AppLayout />}><Route index element={<Navigate to="home" replace />} /><Route path="home" element={<HomePage />} /><Route path="tickets" element={<TicketListPage/>}/><Route path="tickets/new" element={<CreateTicketPage/>}/><Route path="tickets/:id" element={<TicketDetailPage/>}/><Route path="tickets/:id/edit" element={<EditTicketPage/>}/><Route path="notifications" element={<NotificationsPage/>}/></Route></Route>
  <Route path="/" element={<Navigate to="/app/home" replace />} /><Route path="*" element={<NotFoundPage />} />
</Routes> }

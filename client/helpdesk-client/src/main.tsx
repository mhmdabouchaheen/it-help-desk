import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import './index.css'
import App from './App.tsx'
import { AuthProvider } from './auth/AuthProvider.tsx'
import { AppErrorBoundary } from './components/AppErrorBoundary.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AppErrorBoundary><BrowserRouter><AuthProvider><App /></AuthProvider></BrowserRouter></AppErrorBoundary>
  </StrictMode>,
)

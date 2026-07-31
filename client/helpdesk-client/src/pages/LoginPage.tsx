import { useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { ApiProblemError } from '../api/apiClient'
import { useAuth } from '../auth/AuthProvider'
import { ErrorSummary, FieldError } from '../components/Feedback'
import { safeDestination } from '../utils/safeDestination'

export function LoginPage() {
  const auth = useAuth(); const navigate = useNavigate(); const location = useLocation()
  const [email, setEmail] = useState(''); const [password, setPassword] = useState(''); const [busy, setBusy] = useState(false)
  const [errors, setErrors] = useState<Record<string, string>>({}); const [problem, setProblem] = useState<{ message?: string; traceId?: string }>({})
  async function submit(event: FormEvent) {
    event.preventDefault(); if (busy) return
    const next: Record<string, string> = {}
    if (!email) next.email = 'Email is required.'; else if (!/^\S+@\S+\.\S+$/.test(email)) next.email = 'Enter a valid email address.'
    if (!password) next.password = 'Password is required.'
    setErrors(next); if (Object.keys(next).length) return
    setBusy(true); setProblem({})
    try { await auth.login({ email, password }); navigate(safeDestination((location.state as { from?: unknown } | null)?.from), { replace: true }) }
    catch (error) { const api = error instanceof ApiProblemError ? error : null; setProblem({ message: api?.status === 401 ? 'Email or password is incorrect.' : (api?.detail ?? 'Sign in failed. Please try again.'), traceId: api?.traceId }) }
    finally { setBusy(false) }
  }
  return <AuthCard title="Sign in"><form onSubmit={submit} noValidate><ErrorSummary {...problem} />
    <label htmlFor="email">Email</label><input id="email" type="email" autoComplete="email" value={email} onChange={e => setEmail(e.target.value)} aria-invalid={!!errors.email} aria-describedby={errors.email ? 'email-error' : undefined}/><FieldError id="email-error" message={errors.email}/>
    <label htmlFor="password">Password</label><input id="password" type="password" autoComplete="current-password" value={password} onChange={e => setPassword(e.target.value)} aria-invalid={!!errors.password} aria-describedby={errors.password ? 'password-error' : undefined}/><FieldError id="password-error" message={errors.password}/>
    <button disabled={busy}>{busy ? 'Signing in…' : 'Sign in'}</button></form><p>New here? <Link to="/register">Create an account</Link></p></AuthCard>
}
function AuthCard({ title, children }: { title: string; children: React.ReactNode }) { return <main className="auth-page"><section className="auth-card"><h1>{title}</h1>{children}</section></main> }
export { AuthCard }

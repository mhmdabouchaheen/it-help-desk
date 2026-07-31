import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ApiProblemError } from '../api/apiClient'
import { useAuth } from '../auth/AuthProvider'
import { ErrorSummary, FieldError } from '../components/Feedback'
import { AuthCard } from './LoginPage'

export function RegisterPage() {
  const auth = useAuth(); const navigate = useNavigate(); const [busy, setBusy] = useState(false)
  const [form, setForm] = useState({ email: '', displayName: '', password: '', confirmPassword: '' })
  const [errors, setErrors] = useState<Record<string, string>>({}); const [problem, setProblem] = useState<string>()
  const set = (name: keyof typeof form, value: string) => setForm(current => ({ ...current, [name]: value }))
  async function submit(event: FormEvent) {
    event.preventDefault(); if (busy) return; const next: Record<string, string> = {}
    if (!form.email || !/^\S+@\S+\.\S+$/.test(form.email)) next.email = 'Enter a valid email address.'
    if (!form.displayName) next.displayName = 'Display name is required.'; else if (form.displayName.length > 200) next.displayName = 'Display name must be 200 characters or fewer.'
    if (form.password.length < 8) next.password = 'Password must be at least 8 characters.'
    if (form.confirmPassword !== form.password) next.confirmPassword = 'Passwords do not match.'
    setErrors(next); if (Object.keys(next).length) return; setBusy(true); setProblem(undefined)
    try { await auth.register(form); navigate('/app/home', { replace: true }) }
    catch (error) { const api = error instanceof ApiProblemError ? error : null; setProblem(api?.code === 'email_already_registered' ? 'An account with that email already exists.' : (api?.detail ?? 'Registration failed. Please try again.')) }
    finally { setBusy(false) }
  }
  return <AuthCard title="Create account"><form onSubmit={submit} noValidate><ErrorSummary message={problem}/>{(['email','displayName','password','confirmPassword'] as const).map(name => <div className="field" key={name}><label htmlFor={name}>{name === 'displayName' ? 'Display name' : name === 'confirmPassword' ? 'Confirm password' : name[0].toUpperCase()+name.slice(1)}</label><input id={name} type={name.includes('password') || name === 'password' ? 'password' : name === 'email' ? 'email' : 'text'} autoComplete={name === 'email' ? 'email' : name === 'displayName' ? 'name' : name === 'password' ? 'new-password' : 'new-password'} value={form[name]} onChange={e => set(name,e.target.value)} aria-invalid={!!errors[name]} aria-describedby={errors[name] ? `${name}-error` : undefined}/><FieldError id={`${name}-error`} message={errors[name]}/></div>)}<button disabled={busy}>{busy ? 'Creating…' : 'Create account'}</button></form><p>Already registered? <Link to="/login">Sign in</Link></p></AuthCard>
}

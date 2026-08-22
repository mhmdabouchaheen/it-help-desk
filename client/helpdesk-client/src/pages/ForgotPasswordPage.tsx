import {useState,type FormEvent} from 'react'
import {Link} from 'react-router-dom'
import {forgotPasswordAsync} from '../api/authApi'
import {ApiProblemError} from '../api/apiClient'
import {ErrorSummary,FieldError} from '../components/Feedback'
import {AuthCard} from './LoginPage'

const generic='If an account exists for that email, password reset instructions have been sent.'
export function ForgotPasswordPage(){const[email,setEmail]=useState('');const[error,setError]=useState<string>();const[done,setDone]=useState(false);const[busy,setBusy]=useState(false);async function submit(e:FormEvent){e.preventDefault();if(!/^\S+@\S+\.\S+$/.test(email)){setError('Enter a valid email address.');return}setBusy(true);setError(undefined);try{await forgotPasswordAsync({email});setDone(true)}catch(x){const api=x instanceof ApiProblemError?x:null;setError(api?.status===429?'Too many requests. Please try again shortly.':'Password reset could not be requested. Please try again.')}finally{setBusy(false)}}return <AuthCard title="Forgot password">{done?<><p role="status">{generic}</p><p><Link to="/login">Back to sign in</Link></p></>:<form onSubmit={submit} noValidate><ErrorSummary message={error}/><label htmlFor="forgot-email">Email</label><input id="forgot-email" type="email" autoComplete="email" value={email} onChange={e=>setEmail(e.target.value)}/><FieldError id="forgot-email-error" message={error==='Enter a valid email address.'?error:undefined}/><button disabled={busy}>{busy?'Sending…':'Send reset instructions'}</button><p><Link to="/login">Back to sign in</Link></p></form>}</AuthCard>}

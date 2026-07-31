import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { AuthProvider } from '../auth/AuthProvider'
import { LoginPage } from './LoginPage'
import { RegisterPage } from './RegisterPage'
const renderPage=(page:React.ReactNode)=>render(<MemoryRouter><AuthProvider>{page}</AuthProvider></MemoryRouter>)
describe('auth pages',()=>{
  it('validates required login fields',async()=>{renderPage(<LoginPage/>);await userEvent.click(screen.getByRole('button',{name:'Sign in'}));expect(screen.getByText('Email is required.')).toBeInTheDocument();expect(screen.getByText('Password is required.')).toBeInTheDocument()})
  it('rejects invalid email',async()=>{renderPage(<LoginPage/>);await userEvent.type(screen.getByLabelText('Email'),'bad');await userEvent.click(screen.getByRole('button',{name:'Sign in'}));expect(screen.getByText('Enter a valid email address.')).toBeInTheDocument()})
  it('validates password length and confirmation',async()=>{renderPage(<RegisterPage/>);await userEvent.type(screen.getByLabelText('Email'),'a@b.test');await userEvent.type(screen.getByLabelText('Display name'),'User');await userEvent.type(screen.getByLabelText('Password'),'short');await userEvent.type(screen.getByLabelText('Confirm password'),'other');await userEvent.click(screen.getByRole('button',{name:'Create account'}));expect(screen.getByText('Password must be at least 8 characters.')).toBeInTheDocument();expect(screen.getByText('Passwords do not match.')).toBeInTheDocument()})
  it('has no role selector or token output',()=>{renderPage(<RegisterPage/>);expect(screen.queryByLabelText(/role/i)).not.toBeInTheDocument();expect(screen.queryByText(/accessToken|refreshToken/i)).not.toBeInTheDocument();vi.restoreAllMocks()})
})

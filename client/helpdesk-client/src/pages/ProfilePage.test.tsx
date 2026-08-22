import {render,screen,waitFor} from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {describe,expect,it,vi} from 'vitest'
import * as authApi from '../api/authApi'
import {ProfilePage} from './ProfilePage'

const reloadCurrentUser=vi.fn()
const profile={userId:'one',email:'user@example.test',displayName:'Original User',roles:['Employee'],isActive:true}
vi.mock('../auth/AuthProvider',()=>({useAuth:()=>({reloadCurrentUser})}))
vi.mock('../api/authApi',()=>({getProfileAsync:vi.fn(()=>Promise.resolve(profile)),updateProfileAsync:vi.fn(request=>Promise.resolve({...profile,...request})),changePasswordAsync:vi.fn(()=>Promise.resolve())}))
describe('profile page',()=>{it('loads safe fields and shows roles read-only',async()=>{render(<ProfilePage/>);expect(await screen.findByText('user@example.test')).toBeInTheDocument();expect(screen.getByText('Employee')).toBeInTheDocument();expect(screen.queryByLabelText(/role/i)).not.toBeInTheDocument();expect(screen.queryByText(/security stamp|refresh token|password hash/i)).not.toBeInTheDocument()});it('updates display name and reloads current identity',async()=>{render(<ProfilePage/>);const input=await screen.findByLabelText('Display name');await userEvent.clear(input);await userEvent.type(input,'Updated User');await userEvent.click(screen.getByRole('button',{name:'Save profile'}));await waitFor(()=>expect(authApi.updateProfileAsync).toHaveBeenCalledWith({displayName:'Updated User'}));expect(reloadCurrentUser).toHaveBeenCalled()});it('changes password and clears sensitive inputs',async()=>{render(<ProfilePage/>);await screen.findByText('user@example.test');await userEvent.type(screen.getByLabelText('Current password'),'Password1!');await userEvent.type(screen.getByLabelText('New password'),'Password2!');await userEvent.type(screen.getByLabelText('Confirm password'),'Password2!');await userEvent.click(screen.getByRole('button',{name:'Change password'}));await waitFor(()=>expect(authApi.changePasswordAsync).toHaveBeenCalled());expect(screen.getByLabelText('Current password')).toHaveValue('');expect(screen.getByLabelText('New password')).toHaveValue('')})})

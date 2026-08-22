import {render,screen,waitFor} from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {describe,expect,it,vi} from 'vitest'
import * as api from '../api/teamManagement'
import {TeamManagementPage} from './TeamManagementPage'

const users=[{userId:'employee',displayName:'Employee',email:'employee@test',isActive:true,roles:['Employee'],managerUserId:null},{userId:'manager',displayName:'Manager',email:'manager@test',isActive:true,roles:['Manager'],managerUserId:null}]
vi.mock('../api/teamManagement',()=>({getTeamMembersAsync:vi.fn(()=>Promise.resolve(users)),updateUserManagerAsync:vi.fn((id,managerUserId)=>Promise.resolve({...users[0],userId:id,managerUserId,managerDisplayName:'Manager'}))}))
describe('team management',()=>{it('renders users and only active managers as choices',async()=>{render(<TeamManagementPage/>);expect(await screen.findByText('employee@test')).toBeInTheDocument();expect(screen.getByRole('option',{name:'Manager'})).toBeInTheDocument();expect(screen.getByText(/Roles are managed separately/)).toBeInTheDocument()});it('assigns and removes a direct manager',async()=>{render(<TeamManagementPage/>);const select=await screen.findByLabelText('Manager for Employee');await userEvent.selectOptions(select,'manager');await waitFor(()=>expect(api.updateUserManagerAsync).toHaveBeenCalledWith('employee','manager'))})})

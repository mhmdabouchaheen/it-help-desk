import {render,screen,waitFor} from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {describe,expect,it,vi} from 'vitest'
import {ApiProblemError} from '../api/apiClient'
import * as api from '../api/roleManagement'
import {RoleManagementPage} from './RoleManagementPage'

const user={userId:'employee3',displayName:'Employee Three',email:'employee3@test',isActive:true,roles:['Employee'],managerUserId:null}
vi.mock('../api/roleManagement',()=>({getRoleManagedUsersAsync:vi.fn(),updateUserRolesAsync:vi.fn()}))
describe('Role Management',()=>{
  it('renders roles, adds Manager, saves, and refreshes the row',async()=>{vi.mocked(api.getRoleManagedUsersAsync).mockResolvedValue([user]);vi.mocked(api.updateUserRolesAsync).mockResolvedValue({...user,roles:['Employee','Manager']});render(<RoleManagementPage/>);expect(await screen.findByText('employee3@test')).toBeInTheDocument();await userEvent.click(screen.getByLabelText('Manager'));await userEvent.click(screen.getByRole('button',{name:'Save'}));await waitFor(()=>expect(api.updateUserRolesAsync).toHaveBeenCalledWith('employee3',['Employee','Manager']));expect(await screen.findByText('Employee, Manager')).toBeInTheDocument();expect(screen.getByRole('status')).toHaveTextContent('refresh sessions were revoked')})
  it('shows a controlled API error without raw exception details',async()=>{vi.mocked(api.getRoleManagedUsersAsync).mockResolvedValue([user]);vi.mocked(api.updateUserRolesAsync).mockRejectedValue(new ApiProblemError(400,'Role change failed','The final active Admin cannot lose the Admin role.','role_management_failed'));render(<RoleManagementPage/>);await screen.findByText('employee3@test');await userEvent.click(screen.getByRole('button',{name:'Save'}));expect(await screen.findByRole('alert')).toHaveTextContent('final active Admin');expect(screen.queryByText(/stack|exception/i)).not.toBeInTheDocument()})
})

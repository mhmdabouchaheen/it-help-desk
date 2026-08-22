import {apiRequest} from './apiClient'
import type {RoleManagedUserResponse} from '../types/roleManagement'
export const getRoleManagedUsersAsync=(signal?:AbortSignal)=>apiRequest<RoleManagedUserResponse[]>('/api/admin/role-management',{signal})
export const updateUserRolesAsync=(userId:string,roles:string[])=>apiRequest<RoleManagedUserResponse>(`/api/admin/role-management/${userId}/roles`,{method:'PUT',body:JSON.stringify({roles})})

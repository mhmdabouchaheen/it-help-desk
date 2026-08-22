import {apiRequest} from './apiClient'
import type {TeamMemberResponse} from '../types/teamManagement'

export const getTeamMembersAsync=(signal?:AbortSignal)=>apiRequest<TeamMemberResponse[]>('/api/admin/team-members',{signal})
export const updateUserManagerAsync=(userId:string,managerUserId:string|null,signal?:AbortSignal)=>
  apiRequest<TeamMemberResponse>(`/api/admin/team-members/${userId}/manager`,{method:'PUT',body:JSON.stringify({managerUserId}),signal})

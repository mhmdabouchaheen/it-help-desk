export interface TeamMemberResponse {
  userId: string
  displayName: string
  email: string
  isActive: boolean
  roles: string[]
  managerUserId?: string | null
  managerDisplayName?: string | null
}

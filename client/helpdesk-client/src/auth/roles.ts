export const AppRoles = {
  Admin: 'Admin', ItSupportAgent: 'IT Support Agent', Employee: 'Employee', Manager: 'Manager',
} as const
export const RoleGroups = {
  SupportStaff: [AppRoles.Admin, AppRoles.ItSupportAgent],
  Management: [AppRoles.Admin, AppRoles.Manager],
  Reports: [AppRoles.Admin, AppRoles.ItSupportAgent, AppRoles.Manager],
} as const

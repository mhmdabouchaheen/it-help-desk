export const applicationRoles=['Employee','Manager','IT Support Agent','Admin'] as const
export type RoleManagedUserResponse={userId:string;displayName:string;email:string;isActive:boolean;roles:string[];managerUserId:string|null;managerDisplayName?:string|null}

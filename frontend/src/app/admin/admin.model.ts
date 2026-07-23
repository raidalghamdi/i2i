export interface UserRoleSummary {
  roleId: string;
  code: string;
  nameEn: string;
}

export interface AdminUser {
  id: string;
  samAccountName: string;
  email: string;
  fullNameAr: string;
  fullNameEn: string;
  department: string | null;
  title: string | null;
  isActive: boolean;
  roles: UserRoleSummary[];
}

export interface RoleOption {
  id: string;
  code: string;
  nameEn: string;
}

export interface RoleGrantInput {
  samAccountName: string;
  roleCode: string;
}

export interface RoleGrantResult {
  status: string;
  userId: string | null;
  pendingGrantId: string | null;
}

export interface GroupGrantInput {
  groupName: string;
  roleCode: string;
}

export interface GroupGrantResult {
  grantedCount: number;
  pendingCount: number;
  skippedCount: number;
  errors: string[];
}

export interface PendingRoleGrant {
  id: string;
  samAccountName: string;
  roleCode: string;
  roleNameEn: string;
  grantedByName: string;
  grantedAt: string;
}

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  AdminUser,
  GroupGrantInput,
  GroupGrantResult,
  PendingRoleGrant,
  RoleGrantInput,
  RoleGrantResult,
  RoleOption,
} from './admin.model';

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);

  listUsers(): Promise<AdminUser[]> {
    return firstValueFrom(this.http.get<AdminUser[]>('/api/admin/users'));
  }

  getUser(id: string): Promise<AdminUser> {
    return firstValueFrom(this.http.get<AdminUser>(`/api/admin/users/${id}`));
  }

  grantRole(input: RoleGrantInput): Promise<RoleGrantResult> {
    return firstValueFrom(this.http.post<RoleGrantResult>('/api/admin/role-grants', input));
  }

  grantRoleToGroup(input: GroupGrantInput): Promise<GroupGrantResult> {
    return firstValueFrom(this.http.post<GroupGrantResult>('/api/admin/role-grants/group', input));
  }

  revokeRole(userId: string, roleId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/admin/users/${userId}/roles/${roleId}`));
  }

  setActive(userId: string, isActive: boolean): Promise<void> {
    return firstValueFrom(this.http.post<void>(`/api/admin/users/${userId}/active`, { isActive }));
  }

  listPendingGrants(): Promise<PendingRoleGrant[]> {
    return firstValueFrom(this.http.get<PendingRoleGrant[]>('/api/admin/pending-role-grants'));
  }

  cancelPendingGrant(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/admin/pending-role-grants/${id}`));
  }

  listRoles(): Promise<RoleOption[]> {
    return firstValueFrom(this.http.get<RoleOption[]>('/api/roles'));
  }
}

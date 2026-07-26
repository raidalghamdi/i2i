import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { catchError, firstValueFrom, of } from 'rxjs';
import {
  AdminUser,
  DirectoryImportResult,
  GroupGrantInput,
  GroupGrantResult,
  IdeaTemplateInfo,
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

  getCurrentIdeaTemplate(): Promise<IdeaTemplateInfo | null> {
    return firstValueFrom(
      this.http.get<IdeaTemplateInfo>('/api/admin/idea-template').pipe(catchError(() => of(null))),
    );
  }

  uploadIdeaTemplate(file: File): Promise<IdeaTemplateInfo> {
    const formData = new FormData();
    formData.append('file', file);
    return firstValueFrom(this.http.post<IdeaTemplateInfo>('/api/admin/idea-template', formData));
  }

  importFromAd(samAccountNames: string[], roleCode: string): Promise<DirectoryImportResult[]> {
    return firstValueFrom(
      this.http.post<DirectoryImportResult[]>('/api/admin/directory/import', { samAccountNames, roleCode }),
    );
  }
}

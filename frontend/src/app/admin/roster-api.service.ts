import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  BulkCreateResult,
  RoleInvitationSettings,
  RoleInvitationSettingsInput,
  RosterHubRow,
  RosterRoleDetail,
} from './roster.model';

@Injectable({ providedIn: 'root' })
export class RosterApiService {
  private readonly http = inject(HttpClient);

  getHub(): Promise<RosterHubRow[]> {
    return firstValueFrom(this.http.get<RosterHubRow[]>('/api/admin/roster'));
  }

  getRoleDetail(roleCode: string): Promise<RosterRoleDetail> {
    return firstValueFrom(this.http.get<RosterRoleDetail>(`/api/admin/roster/${roleCode}`));
  }

  invite(roleCode: string, samAccountNames: string[], deadlineAt: string | null): Promise<BulkCreateResult> {
    return firstValueFrom(
      this.http.post<BulkCreateResult>(`/api/admin/roster/${roleCode}/invite`, { samAccountNames, deadlineAt }),
    );
  }

  withdraw(id: string): Promise<{ id: string; status: string }> {
    return firstValueFrom(this.http.post<{ id: string; status: string }>(`/api/admin/roster/${id}/withdraw`, {}));
  }

  bulkWithdraw(ids: string[]): Promise<{ withdrawn: number }> {
    return firstValueFrom(this.http.post<{ withdrawn: number }>('/api/admin/roster/withdraw-bulk', { ids }));
  }

  remind(id: string): Promise<{ id: string; reminderCount: number }> {
    return firstValueFrom(this.http.post<{ id: string; reminderCount: number }>(`/api/admin/roster/${id}/remind`, {}));
  }

  bulkRemind(ids: string[]): Promise<{ reminded: number }> {
    return firstValueFrom(this.http.post<{ reminded: number }>('/api/admin/roster/remind-bulk', { ids }));
  }

  getSettings(): Promise<RoleInvitationSettings> {
    return firstValueFrom(this.http.get<RoleInvitationSettings>('/api/admin/roster/settings'));
  }

  updateSettings(input: RoleInvitationSettingsInput): Promise<RoleInvitationSettings> {
    return firstValueFrom(this.http.patch<RoleInvitationSettings>('/api/admin/roster/settings', input));
  }

  importEmployees(rows: { samAccountName: string; roleCode: string }[]): Promise<BulkCreateResult> {
    return firstValueFrom(this.http.post<BulkCreateResult>('/api/admin/employees/import', { rows }));
  }
}

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { InvitationReminderSettings, InvitationReminderSettingsInput } from './invitation-settings.model';

@Injectable({ providedIn: 'root' })
export class InvitationSettingsApiService {
  private readonly http = inject(HttpClient);

  get(): Promise<InvitationReminderSettings> {
    return firstValueFrom(this.http.get<InvitationReminderSettings>('/api/admin/invitations/settings'));
  }

  update(input: InvitationReminderSettingsInput): Promise<InvitationReminderSettings> {
    return firstValueFrom(this.http.patch<InvitationReminderSettings>('/api/admin/invitations/settings', input));
  }
}

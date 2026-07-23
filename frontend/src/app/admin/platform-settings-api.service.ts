import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { SettingRow } from './platform-settings.model';

@Injectable({ providedIn: 'root' })
export class PlatformSettingsApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<SettingRow[]> {
    return firstValueFrom(this.http.get<SettingRow[]>('/api/admin/settings'));
  }

  patch(key: string, valueJson: string): Promise<SettingRow> {
    return firstValueFrom(
      this.http.patch<SettingRow>(`/api/admin/settings/${key}`, { valueJson }),
    );
  }
}

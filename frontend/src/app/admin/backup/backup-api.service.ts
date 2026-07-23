import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BackupApiService {
  private readonly http = inject(HttpClient);

  downloadBackup(): Promise<Blob> {
    return firstValueFrom(this.http.get('/api/admin/backup/export', { responseType: 'blob' }));
  }
}

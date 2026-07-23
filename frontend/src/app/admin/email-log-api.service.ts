import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { EmailLogFilter, EmailLogListResult } from './email-log.model';

@Injectable({ providedIn: 'root' })
export class EmailLogApiService {
  private readonly http = inject(HttpClient);

  list(filter: EmailLogFilter): Promise<EmailLogListResult> {
    let params = new HttpParams();
    if (filter.page !== undefined) params = params.set('page', filter.page);
    if (filter.pageSize !== undefined) params = params.set('pageSize', filter.pageSize);
    if (filter.status) params = params.set('status', filter.status);
    return firstValueFrom(this.http.get<EmailLogListResult>('/api/admin/email-log', { params }));
  }
}

import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuditBrowseResult, AuditFilter } from './audit.model';

@Injectable({ providedIn: 'root' })
export class AuditApiService {
  private readonly http = inject(HttpClient);

  browse(filter: AuditFilter): Promise<AuditBrowseResult> {
    let params = new HttpParams();
    if (filter.entityType) params = params.set('entityType', filter.entityType);
    if (filter.action) params = params.set('action', filter.action);
    if (filter.actorId) params = params.set('actorId', filter.actorId);
    if (filter.from) params = params.set('from', filter.from);
    if (filter.to) params = params.set('to', filter.to);
    if (filter.page !== undefined) params = params.set('page', filter.page);
    if (filter.pageSize !== undefined) params = params.set('pageSize', filter.pageSize);
    return firstValueFrom(this.http.get<AuditBrowseResult>('/api/admin/audit', { params }));
  }
}

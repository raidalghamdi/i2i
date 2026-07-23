import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { SupportFilter, SupportListResult } from './support.model';

@Injectable({ providedIn: 'root' })
export class SupportApiService {
  private readonly http = inject(HttpClient);

  list(filter: SupportFilter): Promise<SupportListResult> {
    let params = new HttpParams();
    if (filter.page !== undefined) params = params.set('page', filter.page);
    if (filter.pageSize !== undefined) params = params.set('pageSize', filter.pageSize);
    if (filter.handled !== undefined) params = params.set('handled', filter.handled);
    return firstValueFrom(this.http.get<SupportListResult>('/api/admin/support', { params }));
  }

  markHandled(id: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`/api/admin/support/${id}/handled`, {}));
  }
}

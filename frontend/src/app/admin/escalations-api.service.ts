import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  Escalation,
  EscalationActionInput,
  EscalationDetail,
  EscalationFilter,
  EscalationResolveInput,
} from './escalations.model';

@Injectable({ providedIn: 'root' })
export class EscalationsApiService {
  private readonly http = inject(HttpClient);

  list(filter: EscalationFilter): Promise<Escalation[]> {
    let params = new HttpParams();
    if (filter.status) params = params.set('status', filter.status);
    if (filter.tier) params = params.set('tier', filter.tier);
    if (filter.entityType) params = params.set('entityType', filter.entityType);
    return firstValueFrom(this.http.get<Escalation[]>('/api/admin/escalations', { params }));
  }

  get(id: string): Promise<EscalationDetail> {
    return firstValueFrom(this.http.get<EscalationDetail>(`/api/admin/escalations/${id}`));
  }

  acknowledge(id: string, input: EscalationActionInput): Promise<Escalation> {
    return firstValueFrom(this.http.post<Escalation>(`/api/admin/escalations/${id}/acknowledge`, input));
  }

  bump(id: string, input: EscalationActionInput): Promise<Escalation> {
    return firstValueFrom(this.http.post<Escalation>(`/api/admin/escalations/${id}/bump`, input));
  }

  resolve(id: string, input: EscalationResolveInput): Promise<Escalation> {
    return firstValueFrom(this.http.post<Escalation>(`/api/admin/escalations/${id}/resolve`, input));
  }
}

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { SlaPolicy, SlaPolicyInput } from './sla-policies.model';

// Change 20260726
@Injectable({ providedIn: 'root' })
export class SlaPoliciesApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<SlaPolicy[]> {
    return firstValueFrom(this.http.get<SlaPolicy[]>('/api/admin/sla-policies'));
  }

  create(input: SlaPolicyInput): Promise<SlaPolicy> {
    return firstValueFrom(this.http.post<SlaPolicy>('/api/admin/sla-policies', input));
  }

  update(id: string, input: SlaPolicyInput): Promise<SlaPolicy> {
    return firstValueFrom(this.http.put<SlaPolicy>(`/api/admin/sla-policies/${id}`, input));
  }

  remove(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/admin/sla-policies/${id}`));
  }
}

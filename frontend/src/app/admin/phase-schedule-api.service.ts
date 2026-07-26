import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { PhaseAnnounceResult, PhaseAudience, PhaseSchedule, PhaseScheduleUpdateInput } from './phase-schedule.model';

@Injectable({ providedIn: 'root' })
export class PhaseScheduleApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<PhaseSchedule[]> {
    return firstValueFrom(this.http.get<PhaseSchedule[]>('/api/admin/phases'));
  }

  update(idx: number, input: PhaseScheduleUpdateInput): Promise<PhaseSchedule> {
    return firstValueFrom(this.http.patch<PhaseSchedule>(`/api/admin/phases/${idx}`, input));
  }

  getAudience(idx: number): Promise<PhaseAudience> {
    return firstValueFrom(this.http.get<PhaseAudience>(`/api/admin/phases/${idx}/audience`));
  }

  setAudience(idx: number, roleCodes: string[]): Promise<PhaseAudience> {
    return firstValueFrom(this.http.put<PhaseAudience>(`/api/admin/phases/${idx}/audience`, { roleCodes }));
  }

  announce(idx: number): Promise<PhaseAnnounceResult> {
    return firstValueFrom(this.http.post<PhaseAnnounceResult>(`/api/admin/phases/${idx}/announce`, {}));
  }
}

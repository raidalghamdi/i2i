import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { PhaseSchedule, PhaseScheduleUpdateInput } from './phase-schedule.model';

@Injectable({ providedIn: 'root' })
export class PhaseScheduleApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<PhaseSchedule[]> {
    return firstValueFrom(this.http.get<PhaseSchedule[]>('/api/admin/phases'));
  }

  update(idx: number, input: PhaseScheduleUpdateInput): Promise<PhaseSchedule> {
    return firstValueFrom(this.http.patch<PhaseSchedule>(`/api/admin/phases/${idx}`, input));
  }
}

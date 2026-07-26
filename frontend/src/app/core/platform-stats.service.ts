import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface PlatformStats {
  totalIdeas: number;
  totalApproved: number;
  totalSubmitters: number;
  totalEvaluations: number;
  totalEvaluators: number;
}

@Injectable({ providedIn: 'root' })
export class PlatformStatsService {
  private readonly http = inject(HttpClient);

  get(): Promise<PlatformStats> {
    return firstValueFrom(this.http.get<PlatformStats>('/api/platform-stats'));
  }
}

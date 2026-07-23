import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ReportTitle, ReportTitleInput, ReportTitlePatch } from './report-titles.model';

@Injectable({ providedIn: 'root' })
export class ReportTitlesApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<ReportTitle[]> {
    return firstValueFrom(this.http.get<ReportTitle[]>('/api/admin/report-titles'));
  }

  create(input: ReportTitleInput): Promise<{ id: string }> {
    return firstValueFrom(this.http.post<{ id: string }>('/api/admin/report-titles', input));
  }

  update(id: string, patch: ReportTitlePatch): Promise<void> {
    return firstValueFrom(this.http.put<void>(`/api/admin/report-titles/${id}`, patch));
  }

  remove(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/admin/report-titles/${id}`));
  }
}

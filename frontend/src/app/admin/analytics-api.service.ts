import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { catchError, firstValueFrom, of } from 'rxjs';
import { AnalyticsDashboard, ExecutiveAnalytics, PillarDetail } from './analytics.model';
import { ReportsApiService } from './reports-api.service';

@Injectable({ providedIn: 'root' })
export class AnalyticsApiService {
  private readonly http = inject(HttpClient);
  private readonly reportsApi = inject(ReportsApiService);

  getDashboard(): Promise<AnalyticsDashboard> {
    return firstValueFrom(this.http.get<AnalyticsDashboard>('/api/admin/analytics'));
  }

  getExecutive(): Promise<ExecutiveAnalytics> {
    return firstValueFrom(this.http.get<ExecutiveAnalytics>('/api/analytics/executive'));
  }

  getPillar(themeId: string): Promise<PillarDetail | null> {
    return firstValueFrom(
      this.http.get<PillarDetail>(`/api/analytics/pillars/${themeId}`).pipe(catchError(() => of(null))),
    );
  }

  exportAnalytics(format: 'xlsx' | 'pdf' | 'pptx'): Promise<{ reportGenerationId: string; status: string }> {
    const params = new HttpParams().set('format', format);
    return firstValueFrom(
      this.http.post<{ reportGenerationId: string; status: string }>('/api/admin/analytics/export', null, { params }),
    );
  }

  downloadReport(id: string): Promise<Blob> {
    return this.reportsApi.downloadReport(id);
  }
}

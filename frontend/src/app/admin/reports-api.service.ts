import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ReportGenerationResult } from './reports.model';

@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private readonly http = inject(HttpClient);

  generateAuditLogReport(): Promise<ReportGenerationResult> {
    return firstValueFrom(this.http.post<ReportGenerationResult>('/api/admin/reports/audit-log/generate', null));
  }

  generateIdeasReport(): Promise<ReportGenerationResult> {
    return firstValueFrom(this.http.post<ReportGenerationResult>('/api/admin/reports/ideas/generate', null));
  }

  generateEvaluationsReport(): Promise<ReportGenerationResult> {
    return firstValueFrom(this.http.post<ReportGenerationResult>('/api/admin/reports/evaluations/generate', null));
  }

  generateEscalationsReport(): Promise<ReportGenerationResult> {
    return firstValueFrom(this.http.post<ReportGenerationResult>('/api/admin/reports/escalations/generate', null));
  }

  downloadReport(id: string): Promise<Blob> {
    return firstValueFrom(this.http.get(`/api/admin/reports/${id}/download`, { responseType: 'blob' }));
  }

  generateReport(
    type: string,
    opts?: { from?: string; to?: string; themeId?: string; format?: string },
  ): Promise<ReportGenerationResult> {
    let params = new HttpParams().set('type', type).set('format', opts?.format ?? 'xlsx');
    if (opts?.from) params = params.set('from', opts.from);
    if (opts?.to) params = params.set('to', opts.to);
    if (opts?.themeId) params = params.set('themeId', opts.themeId);
    return firstValueFrom(
      this.http.post<ReportGenerationResult>('/api/admin/reports/generate', null, { params }),
    );
  }

  exportAnalytics(format: string): Promise<ReportGenerationResult> {
    const params = new HttpParams().set('format', format);
    return firstValueFrom(
      this.http.post<ReportGenerationResult>('/api/admin/analytics/export', null, { params }),
    );
  }
}

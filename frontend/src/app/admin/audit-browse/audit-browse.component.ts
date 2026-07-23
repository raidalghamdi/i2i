import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { AuditApiService } from '../audit-api.service';
import { AuditRow } from '../audit.model';
import { ReportsApiService } from '../reports-api.service';

@Component({
  selector: 'app-audit-browse',
  imports: [DatePipe, FormsModule, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './audit-browse.component.html',
})
export class AuditBrowseComponent implements OnInit {
  private readonly auditApi = inject(AuditApiService);
  private readonly reportsApi = inject(ReportsApiService);

  readonly pageSize = 25;

  readonly rows = signal<AuditRow[]>([]);
  readonly total = signal<number>(0);
  readonly page = signal<number>(1);

  readonly entityType = signal<string>('');
  readonly action = signal<string>('');
  readonly actorId = signal<string>('');
  readonly from = signal<string>('');
  readonly to = signal<string>('');

  readonly loading = signal<boolean>(false);
  readonly loadError = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly exporting = signal<boolean>(false);

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.loadError.set(null);
    this.loading.set(true);
    try {
      const p = await this.auditApi.browse({
        entityType: this.entityType() || undefined,
        action: this.action() || undefined,
        actorId: this.actorId() || undefined,
        from: this.from() || undefined,
        to: this.to() || undefined,
        page: this.page(),
        pageSize: this.pageSize,
      });
      this.rows.set(p.items);
      this.total.set(p.total);
    } catch (error) {
      this.loadError.set(this.extractErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  async onFilterChange(): Promise<void> {
    this.page.set(1);
    await this.reload();
  }

  totalPages(): number {
    return Math.max(1, Math.ceil(this.total() / this.pageSize));
  }

  async onPrev(): Promise<void> {
    if (this.page() <= 1) return;
    this.page.update((p) => p - 1);
    await this.reload();
  }

  async onNext(): Promise<void> {
    if (this.page() * this.pageSize >= this.total()) return;
    this.page.update((p) => p + 1);
    await this.reload();
  }

  async onExport(): Promise<void> {
    this.errorMessage.set(null);
    this.exporting.set(true);
    try {
      const result = await this.reportsApi.generateAuditLogReport();
      if (result.status !== 'completed') {
        this.errorMessage.set($localize`Report generation failed. Please try again.`);
        return;
      }
      const blob = await this.reportsApi.downloadReport(result.reportGenerationId);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = 'audit-log.xlsx';
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.exporting.set(false);
    }
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`Something went wrong. Please try again.`;
  }
}

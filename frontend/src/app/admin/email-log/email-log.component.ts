import { DatePipe } from '@angular/common';
import { Component, Inject, LOCALE_ID, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { EmailLogApiService } from '../email-log-api.service';
import { EmailLogRow } from '../email-log.model';

@Component({
  selector: 'app-email-log',
  imports: [
    DatePipe,
    FormsModule,
    PageHeaderComponent,
    StatusBadgeComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  templateUrl: './email-log.component.html',
})
export class EmailLogComponent implements OnInit {
  private readonly emailLogApi = inject(EmailLogApiService);
  private readonly isArabic: boolean;

  readonly pageSize = 25;

  readonly rows = signal<EmailLogRow[]>([]);
  readonly total = signal<number>(0);
  readonly page = signal<number>(1);
  readonly statusFilter = signal<string>('');

  readonly loading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.errorMessage.set(null);
    this.loading.set(true);
    try {
      const p = await this.emailLogApi.list({
        status: this.statusFilter() || undefined,
        page: this.page(),
        pageSize: this.pageSize,
      });
      this.rows.set(p.items);
      this.total.set(p.total);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  async onFilterChange(): Promise<void> {
    this.page.set(1);
    await this.reload();
  }

  statusName(row: EmailLogRow): string {
    return this.isArabic ? row.statusNameAr : row.statusNameEn;
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

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@emailLogLoadError:Could not load the email log. Please try again.`;
  }
}

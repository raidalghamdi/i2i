import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { SupportApiService } from '../support-api.service';
import { SupportRow } from '../support.model';

@Component({
  selector: 'app-support-inbox',
  imports: [DatePipe, FormsModule, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './support-inbox.component.html',
})
export class SupportInboxComponent implements OnInit {
  private readonly supportApi = inject(SupportApiService);

  readonly pageSize = 25;

  readonly rows = signal<SupportRow[]>([]);
  readonly total = signal<number>(0);
  readonly page = signal<number>(1);
  readonly handledFilter = signal<string>('');

  readonly loading = signal<boolean>(false);
  readonly loadError = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.loadError.set(null);
    this.loading.set(true);
    try {
      const p = await this.supportApi.list({
        handled: this.handledFilter() === '' ? undefined : this.handledFilter() === 'true',
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

  async onMarkHandled(id: string): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.supportApi.markHandled(id);
      await this.reload();
    } catch {
      this.errorMessage.set(
        $localize`:@@supportHandleError:Could not mark as handled. Please try again.`,
      );
    }
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@supportLoadError:Could not load the support inbox. Please try again.`;
  }
}

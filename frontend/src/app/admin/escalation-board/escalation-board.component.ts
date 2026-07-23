import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { EscalationsApiService } from '../escalations-api.service';
import { Escalation } from '../escalations.model';

@Component({
  selector: 'app-escalation-board',
  imports: [
    RouterLink,
    FormsModule,
    PageHeaderComponent,
    StatusBadgeComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  templateUrl: './escalation-board.component.html',
})
export class EscalationBoardComponent implements OnInit {
  private readonly escalationsApi = inject(EscalationsApiService);

  readonly escalations = signal<Escalation[]>([]);
  readonly statusFilter = signal<string>('open');
  readonly tierFilter = signal<string>('');
  readonly entityTypeFilter = signal<string>('');
  readonly resolutionAr = signal<string>('');
  readonly resolutionEn = signal<string>('');
  readonly loading = signal<boolean>(false);
  readonly loadError = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    await this.onFilterChange();
  }

  async onFilterChange(): Promise<void> {
    this.loadError.set(null);
    this.loading.set(true);
    try {
      this.escalations.set(
        await this.escalationsApi.list({
          status: this.statusFilter() || undefined,
          tier: this.tierFilter() || undefined,
          entityType: this.entityTypeFilter() || undefined,
        }),
      );
    } catch (error) {
      this.loadError.set(this.extractErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  async onAcknowledge(id: string): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.escalationsApi.acknowledge(id, { notes: null });
      await this.onFilterChange();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onBump(id: string): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.escalationsApi.bump(id, { notes: null });
      await this.onFilterChange();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onResolve(id: string): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.escalationsApi.resolve(id, { resolutionAr: this.resolutionAr(), resolutionEn: this.resolutionEn() });
      this.resolutionAr.set('');
      this.resolutionEn.set('');
      await this.onFilterChange();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
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

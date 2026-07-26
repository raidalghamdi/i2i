import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { EvaluationReviewApiService } from '../../evaluations/evaluation-review-api.service';
import { SupervisorQueueItem } from '../supervisor.model';

@Component({
  selector: 'app-committee-pending-queue',
  imports: [RouterLink, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './committee-pending-queue.component.html',
})
export class CommitteePendingQueueComponent implements OnInit {
  private readonly evaluationReviewApi = inject(EvaluationReviewApiService);

  readonly queue = signal<SupervisorQueueItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): Promise<void> {
    return this.load();
  }

  reload(): Promise<void> {
    return this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.queue.set(await this.evaluationReviewApi.getCommitteePendingQueue());
    } catch (error) {
      this.error.set(this.extractErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@committeePendingQueueLoadError:Couldn't load the committee-pending queue. Please try again.`;
  }
}

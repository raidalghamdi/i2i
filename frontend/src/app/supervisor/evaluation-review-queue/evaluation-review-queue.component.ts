import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { EvaluationReviewApiService } from '../../evaluations/evaluation-review-api.service';
import { SupervisorQueueItem } from '../supervisor.model';

@Component({
  selector: 'app-evaluation-review-queue',
  imports: [RouterLink, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './evaluation-review-queue.component.html',
})
export class EvaluationReviewQueueComponent implements OnInit {
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
      this.queue.set(await this.evaluationReviewApi.getReviewQueue());
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
    return $localize`:@@evaluationReviewQueueLoadError:Couldn't load the evaluation review queue. Please try again.`;
  }
}

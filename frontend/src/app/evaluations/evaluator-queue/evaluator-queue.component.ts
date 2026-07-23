import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EvaluationsApiService } from '../evaluations-api.service';
import { EvaluationQueueItem } from '../evaluation.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';

@Component({
  selector: 'app-evaluator-queue',
  imports: [RouterLink, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './evaluator-queue.component.html',
})
export class EvaluatorQueueComponent implements OnInit {
  private readonly evaluationsApi = inject(EvaluationsApiService);
  readonly queue = signal<EvaluationQueueItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  reload(): void {
    void this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.queue.set(await this.evaluationsApi.getQueue());
    } catch {
      this.error.set($localize`:@@evaluatorQueueLoadError:Couldn't load the evaluation queue. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }
}

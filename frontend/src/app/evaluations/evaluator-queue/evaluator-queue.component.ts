import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EvaluationsApiService } from '../evaluations-api.service';
import { EvaluationQueueItem } from '../evaluation.model';
import { OwnIdeasService } from '../../core/own-ideas.service'; // Change 20260726
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
  private readonly ownIdeas = inject(OwnIdeasService); // Change 20260726
  readonly queue = signal<EvaluationQueueItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  private readonly ownIdeaIds = signal<Set<string>>(new Set()); // Change 20260726

  // Change 20260726
  readonly ownIdeaTooltip = $localize`:@@evaluatorQueueOwnIdeaTooltip:You cannot evaluate your own idea`;

  /** An evaluator may not evaluate an idea they submitted; the backend rejects it too. */
  // Change 20260726
  isOwnIdea(ideaId: string): boolean {
    return this.ownIdeaIds().has(ideaId);
  }

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
      const [queue, ownIdeaIds] = await Promise.all([ // Change 20260726
        this.evaluationsApi.getQueue(),
        this.ownIdeas.loadOwnIdeaIds(),
      ]);
      this.queue.set(queue);
      this.ownIdeaIds.set(ownIdeaIds);
    } catch {
      this.error.set($localize`:@@evaluatorQueueLoadError:Couldn't load the evaluation queue. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }
}

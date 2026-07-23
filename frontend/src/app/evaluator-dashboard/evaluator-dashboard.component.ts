import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EvaluationsApiService } from '../evaluations/evaluations-api.service';
import { EvaluationQueueItem, MyEvaluation } from '../evaluations/evaluation.model';
import { MeApiService } from '../core/me-api.service';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../shared/error-state/error-state.component';

@Component({
  selector: 'app-evaluator-dashboard',
  imports: [RouterLink, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './evaluator-dashboard.component.html',
})
export class EvaluatorDashboardComponent implements OnInit {
  private readonly evaluationsApi = inject(EvaluationsApiService);
  private readonly meApi = inject(MeApiService);

  readonly queue = signal<EvaluationQueueItem[]>([]);
  readonly mine = signal<MyEvaluation[]>([]);
  readonly points = signal(0);
  readonly level = signal(1);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly awaitingCount = computed(() => this.queue().length);

  readonly evaluatedThisMonthCount = computed(() => {
    const now = new Date();
    return this.mine().filter((m) => {
      const d = new Date(m.submittedAt);
      return d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth();
    }).length;
  });

  readonly avgDays = computed<number | null>(() => {
    const durations = this.mine()
      .filter((m) => m.ideaEnteredEvaluationAt !== null)
      .map((m) => (new Date(m.submittedAt).getTime() - new Date(m.ideaEnteredEvaluationAt!).getTime()) / 86_400_000)
      .filter((d) => d >= 0);
    if (durations.length === 0) return null;
    return Math.max(1, Math.round(durations.reduce((a, b) => a + b, 0) / durations.length));
  });

  readonly queuePreview = computed(() =>
    [...this.queue()].sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()).slice(0, 5)
  );

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
      const [queue, mine, me] = await Promise.all([
        this.evaluationsApi.getQueue(),
        this.evaluationsApi.getMine(),
        this.meApi.get(),
      ]);
      this.queue.set(queue);
      this.mine.set(mine);
      this.points.set(me.points);
      this.level.set(me.level);
    } catch {
      this.error.set($localize`:@@evaluatorDashboardLoadError:Couldn't load your evaluator dashboard.`);
    } finally {
      this.loading.set(false);
    }
  }
}

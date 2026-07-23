import { Component, OnInit, inject, signal } from '@angular/core';
import { EvaluationsApiService } from '../evaluations-api.service';
import { MyEvaluation } from '../evaluation.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';

@Component({
  selector: 'app-my-evaluations-list',
  imports: [PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './my-evaluations-list.component.html',
})
export class MyEvaluationsListComponent implements OnInit {
  private readonly evaluationsApi = inject(EvaluationsApiService);
  readonly evaluations = signal<MyEvaluation[]>([]);
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
      this.evaluations.set(await this.evaluationsApi.getMine());
    } catch {
      this.error.set($localize`:@@myEvaluationsLoadError:Couldn't load your evaluations. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }
}

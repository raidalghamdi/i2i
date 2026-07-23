import { Component, inject, signal } from '@angular/core';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { AssignmentApiService } from '../assignment-api.service';
import { SuggestedEvaluator } from '../assignment.model';

@Component({
  selector: 'app-evaluator-auto-suggest',
  imports: [LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './evaluator-auto-suggest.component.html',
})
export class EvaluatorAutoSuggestComponent {
  private readonly api = inject(AssignmentApiService);

  readonly suggestions = signal<SuggestedEvaluator[]>([]);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly hasSearched = signal(false);

  async onSuggestClick(): Promise<void> {
    this.errorMessage.set(null);
    this.loading.set(true);
    try {
      this.suggestions.set(await this.api.suggestEvaluators());
      this.hasSearched.set(true);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@autoSuggestError:Could not fetch suggestions. Please try again.`;
  }
}

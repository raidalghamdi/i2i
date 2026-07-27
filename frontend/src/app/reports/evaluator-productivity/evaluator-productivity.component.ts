import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { EvaluatorProductivityApiService } from '../evaluator-productivity-api.service';
import {
  EvaluatorProductivityRow,
  EvaluatorProductivitySortKey,
} from '../evaluator-productivity.model';

// Change 20260726
@Component({
  selector: 'app-evaluator-productivity',
  imports: [DecimalPipe, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './evaluator-productivity.component.html',
})
export class EvaluatorProductivityComponent implements OnInit {
  private readonly api = inject(EvaluatorProductivityApiService);

  readonly rows = signal<EvaluatorProductivityRow[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly sortKey = signal<EvaluatorProductivitySortKey>('completedCount');
  readonly sortDescending = signal(true);

  readonly sortedRows = computed(() => {
    const key = this.sortKey();
    const direction = this.sortDescending() ? -1 : 1;
    return [...this.rows()].sort((a, b) => {
      const left = a[key];
      const right = b[key];
      // Nulls (no submissions yet, so no average) stay last in both directions.
      if (left === null && right === null) return 0;
      if (left === null) return 1;
      if (right === null) return -1;
      return this.compare(left, right) * direction;
    });
  });

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      this.rows.set(await this.api.list());
    } catch (error) {
      this.loadError.set(this.extractErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  onSort(key: EvaluatorProductivitySortKey): void {
    if (this.sortKey() === key) {
      this.sortDescending.update((descending) => !descending);
      return;
    }
    this.sortKey.set(key);
    // Names read naturally A→Z; every other column is a count or an average where
    // "most first" is the useful default.
    this.sortDescending.set(key !== 'displayName');
  }

  ariaSort(key: EvaluatorProductivitySortKey): 'ascending' | 'descending' | 'none' {
    if (this.sortKey() !== key) return 'none';
    return this.sortDescending() ? 'descending' : 'ascending';
  }

  private compare(a: string | number, b: string | number): number {
    if (typeof a === 'string' && typeof b === 'string') return a.localeCompare(b);
    return Number(a) - Number(b);
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`Something went wrong. Please try again.`;
  }
}

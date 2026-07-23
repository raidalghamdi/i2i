import { Component, EventEmitter, OnInit, Output, inject, signal } from '@angular/core';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { AssignmentApiService } from '../assignment-api.service';
import { WorkloadRow } from '../assignment.model';

type Bucket = 'pending' | 'dueSoon' | 'overdue' | 'completedRecent';

@Component({
  selector: 'app-workload-heatmap',
  imports: [LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './workload-heatmap.component.html',
})
export class WorkloadHeatmapComponent implements OnInit {
  private readonly api = inject(AssignmentApiService);

  @Output() readonly cellClicked = new EventEmitter<{ evaluatorId: string; status: string }>();

  readonly rows = signal<WorkloadRow[]>([]);
  readonly buckets: Bucket[] = ['pending', 'dueSoon', 'overdue', 'completedRecent'];

  readonly loading = signal<boolean>(false);
  readonly loadError = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.loadError.set(null);
    this.loading.set(true);
    try {
      this.rows.set(await this.api.getWorkloadHeatmap());
    } catch (error) {
      this.loadError.set(this.extractErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  countFor(row: WorkloadRow, bucket: Bucket): number {
    return row[bucket];
  }

  shadeClass(count: number): string {
    if (count === 0) return 'bg-muted';
    if (count <= 2) return 'bg-brand-teal-light';
    if (count <= 4) return 'bg-brand-teal/40';
    if (count <= 6) return 'bg-brand-teal/70';
    return 'bg-brand-teal text-white';
  }

  onCellClick(evaluatorId: string, bucket: Bucket): void {
    const status = bucket === 'dueSoon' || bucket === 'overdue' ? 'pending' : bucket === 'completedRecent' ? 'completed' : bucket;
    this.cellClicked.emit({ evaluatorId, status });
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@workloadHeatmapLoadError:Could not load the workload heatmap. Please try again.`;
  }
}

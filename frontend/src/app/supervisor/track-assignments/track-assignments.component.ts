import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StrategicThemesService } from '../../ideas/strategic-themes.service';
import { StrategicTheme } from '../../ideas/idea.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { SupervisorApiService } from '../supervisor-api.service';
import { RoleUser, TrackAssignment } from '../supervisor.model';

@Component({
  selector: 'app-track-assignments',
  imports: [FormsModule, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './track-assignments.component.html',
})
export class TrackAssignmentsComponent implements OnInit {
  private readonly supervisorApi = inject(SupervisorApiService);
  private readonly themesApi = inject(StrategicThemesService);

  readonly assignments = signal<TrackAssignment[]>([]);
  readonly evaluators = signal<RoleUser[]>([]);
  readonly themes = signal<StrategicTheme[]>([]);
  readonly selectedEvaluatorId = signal<string>('');
  readonly selectedTrackId = signal<string>('');
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);

  ngOnInit(): Promise<void> {
    return this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      this.assignments.set(await this.supervisorApi.getTrackAssignments());
      this.evaluators.set(await this.supervisorApi.getUsersByRole('evaluator'));
      this.themes.set(await this.themesApi.list());
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@trackAssignmentsLoadError:Couldn't load track assignments. Please try again.`),
      );
    } finally {
      this.loading.set(false);
    }
  }

  async onAssign(): Promise<void> {
    const evaluatorId = this.selectedEvaluatorId();
    const trackId = this.selectedTrackId();
    if (!evaluatorId || !trackId) return;
    this.errorMessage.set(null);
    try {
      await this.supervisorApi.createTrackAssignment({ evaluatorId, trackId });
      this.assignments.set(await this.supervisorApi.getTrackAssignments());
      this.selectedEvaluatorId.set('');
      this.selectedTrackId.set('');
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onRemove(id: string): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.supervisorApi.removeTrackAssignment(id);
      this.assignments.set(await this.supervisorApi.getTrackAssignments());
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  private extractErrorMessage(error: unknown, fallback = $localize`Something went wrong. Please try again.`): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return fallback;
  }
}

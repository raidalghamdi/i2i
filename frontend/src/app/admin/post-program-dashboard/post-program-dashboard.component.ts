import { Component, OnInit, inject, signal } from '@angular/core';
import { PostProgramApiService } from '../post-program-api.service';
import { PostProgramIdea } from '../post-program.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';

const NEXT_STAGE: Record<string, string | null> = {
  approved: 'in_pilot',
  in_pilot: 'in_measurement',
  in_measurement: 'in_scaling',
  in_scaling: null,
};

@Component({
  selector: 'app-post-program-dashboard',
  imports: [PageHeaderComponent, StatusBadgeComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './post-program-dashboard.component.html',
})
export class PostProgramDashboardComponent implements OnInit {
  private readonly api = inject(PostProgramApiService);

  readonly ideas = signal<PostProgramIdea[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);

  ngOnInit(): Promise<void> {
    return this.refresh();
  }

  async refresh(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      this.ideas.set(await this.api.getIdeas());
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@postProgramDashboardLoadError:Couldn't load post-program ideas. Please try again.`),
      );
    } finally {
      this.loading.set(false);
    }
  }

  nextStage(status: string): string | null {
    return NEXT_STAGE[status] ?? null;
  }

  async onAdvance(ideaId: string, stage: string): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.api.advance(ideaId, stage);
      await this.refresh();
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

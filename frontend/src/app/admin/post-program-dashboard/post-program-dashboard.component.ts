import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PostProgramApiService } from '../post-program-api.service';
import { PostProgramHistoryEntry, PostProgramIdea } from '../post-program.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { StatusLabelPipe } from '../../shared/status-label/status-label.pipe';

const NEXT_STAGE: Record<string, string | null> = {
  approved: 'in_pilot',
  in_pilot: 'in_measurement',
  in_measurement: 'in_scaling',
  in_scaling: null,
};

@Component({
  selector: 'app-post-program-dashboard',
  imports: [FormsModule, DatePipe, PageHeaderComponent, StatusBadgeComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent, StatusLabelPipe],
  templateUrl: './post-program-dashboard.component.html',
})
export class PostProgramDashboardComponent implements OnInit {
  private readonly api = inject(PostProgramApiService);

  readonly ideas = signal<PostProgramIdea[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  /** Draft advance-comment per idea id, keyed while the row's textarea is being edited. */
  readonly comments = signal<Record<string, string>>({});
  /** Loaded transition history per idea id, populated lazily on expand. */
  readonly historyByIdea = signal<Record<string, PostProgramHistoryEntry[]>>({});
  readonly expandedIdeaId = signal<string | null>(null);
  readonly historyError = signal<string | null>(null);

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

  commentFor(ideaId: string): string {
    return this.comments()[ideaId] ?? '';
  }

  setComment(ideaId: string, value: string): void {
    this.comments.update((c) => ({ ...c, [ideaId]: value }));
  }

  async onAdvance(ideaId: string, stage: string, comment: string): Promise<void> {
    if (!comment.trim()) return;
    this.errorMessage.set(null);
    try {
      await this.api.advance(ideaId, stage, comment);
      this.comments.update((c) => {
        const next = { ...c };
        delete next[ideaId];
        return next;
      });
      await this.refresh();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  /** Toggles the transition-history panel for an idea, loading it lazily on first expand. */
  async toggleHistory(ideaId: string): Promise<void> {
    if (this.expandedIdeaId() === ideaId) {
      this.expandedIdeaId.set(null);
      return;
    }
    this.expandedIdeaId.set(ideaId);
    if (this.historyByIdea()[ideaId]) return;

    this.historyError.set(null);
    try {
      const history = await this.api.getHistory(ideaId);
      this.historyByIdea.update((h) => ({ ...h, [ideaId]: history }));
    } catch (error) {
      this.historyError.set(this.extractErrorMessage(error));
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

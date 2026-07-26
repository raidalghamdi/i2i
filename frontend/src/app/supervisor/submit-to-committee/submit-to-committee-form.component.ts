import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IdeasApiService } from '../../ideas/ideas-api.service';
import { Idea } from '../../ideas/idea.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { IdeaContextPanelComponent } from '../../shared/idea-context-panel/idea-context-panel.component';
import { RoleUserMultiselectComponent } from '../../shared/role-user-multiselect/role-user-multiselect.component';
import { SupervisorApiService } from '../supervisor-api.service';
import { RoleUser } from '../supervisor.model';
import { CommitteeApiService } from '../../committee/committee-api.service';

@Component({
  selector: 'app-submit-to-committee-form',
  imports: [
    PageHeaderComponent,
    LoadingStateComponent,
    ErrorStateComponent,
    IdeaContextPanelComponent,
    RoleUserMultiselectComponent,
  ],
  templateUrl: './submit-to-committee-form.component.html',
})
export class SubmitToCommitteeFormComponent implements OnInit {
  private readonly ideasApi = inject(IdeasApiService);
  private readonly supervisorApi = inject(SupervisorApiService);
  private readonly committeeApi = inject(CommitteeApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly idea = signal<Idea | null>(null);
  readonly judges = signal<RoleUser[]>([]);
  readonly selectedJudgeIds = signal<string[]>([]);
  readonly judgeSelectionError = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  private readonly ideaId = this.route.snapshot.paramMap.get('id')!;

  ngOnInit(): Promise<void> {
    return this.load();
  }

  reload(): Promise<void> {
    return this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const [idea, judges] = await Promise.all([
        this.ideasApi.getById(this.ideaId),
        this.supervisorApi.getUsersByRole('judge'),
      ]);
      this.idea.set(idea);
      this.judges.set(judges);
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(
          error,
          $localize`:@@submitToCommitteeFormLoadError:Couldn't load this idea. Please try again.`,
        ),
      );
    } finally {
      this.loading.set(false);
    }
  }

  async onSubmit(): Promise<void> {
    this.judgeSelectionError.set(null);
    if (this.selectedJudgeIds().length === 0) {
      this.judgeSelectionError.set($localize`:@@submitToCommitteeFormJudgeRequired:Select at least one judge.`);
      return;
    }
    this.errorMessage.set(null);

    try {
      await this.committeeApi.submitToCommittee(this.ideaId, this.selectedJudgeIds());
      await this.router.navigate(['/supervisor/committee-pending']);
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

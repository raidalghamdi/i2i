import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EvaluationSummary, IdeasApiService } from '../ideas-api.service';
import { Idea, IdeaJourney } from '../idea.model';
import { CommitteeApiService } from '../../committee/committee-api.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { StrategicThemesService } from '../strategic-themes.service';
import { ActivitiesService } from '../activities.service';
import { ChallengesService } from '../challenges.service';
import { IdeaHeroComponent } from '../idea-hero/idea-hero.component';
import { MeApiService } from '../../core/me-api.service';
import { PostProgramStepperComponent } from '../post-program-stepper/post-program-stepper.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';

@Component({
  selector: 'app-idea-detail',
  imports: [
    RouterLink,
    PageHeaderComponent,
    StatusBadgeComponent,
    IdeaHeroComponent,
    PostProgramStepperComponent,
    LoadingStateComponent,
    ErrorStateComponent,
  ],
  templateUrl: './idea-detail.component.html',
})
export class IdeaDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly ideasApi = inject(IdeasApiService);
  private readonly committeeApi = inject(CommitteeApiService);
  private readonly themesApi = inject(StrategicThemesService);
  private readonly activitiesApi = inject(ActivitiesService);
  private readonly challengesApi = inject(ChallengesService);
  private readonly meApi = inject(MeApiService);

  private ideaId!: string;

  readonly idea = signal<Idea | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly queuedFiles = signal<File[]>([]);
  readonly evaluationSummary = signal<EvaluationSummary | null>(null);
  readonly journey = signal<IdeaJourney | null>(null);
  readonly trackName = signal<string | null>(null);
  readonly activityName = signal<string | null>(null);
  readonly challengeText = signal<string | null>(null);
  readonly currentUserId = signal<string | null>(null);

  readonly isOwner = computed(() => {
    const idea = this.idea();
    return !!idea && this.currentUserId() === idea.submitterId;
  });

  readonly showPostProgram = computed(() => {
    const idea = this.idea();
    if (!idea || !this.isOwner()) return false;
    return ['approved', 'in_pilot', 'in_measurement', 'in_scaling'].includes(idea.status);
  });

  async ngOnInit(): Promise<void> {
    this.ideaId = this.route.snapshot.paramMap.get('id')!;
    await this.load();
  }

  async reload(): Promise<void> {
    await this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const id = this.ideaId;
      const idea = await this.ideasApi.getById(id);
      this.idea.set(idea);

      if (['approved', 'in_pilot', 'in_measurement', 'in_scaling'].includes(idea.status)) {
        try {
          const me = await this.meApi.get();
          this.currentUserId.set(me.id);
        } catch {
          this.currentUserId.set(null);
        }
      }

      try {
        this.journey.set(await this.ideasApi.getJourney(id));
      } catch {
        this.journey.set(null);
      }

      const [themes, activities] = await Promise.all([this.themesApi.list(), this.activitiesApi.list()]);
      this.trackName.set(themes.find((t) => t.id === idea.strategicThemeId)?.nameEn ?? null);
      this.activityName.set(activities.find((a) => a.id === idea.activityId)?.nameEn ?? null);
      if (idea.challengeId) {
        const challenges = await this.challengesApi.listByTheme(idea.strategicThemeId);
        this.challengeText.set(challenges.find((c) => c.id === idea.challengeId)?.textEn ?? null);
      }

      if (idea.status === 'pass_awaiting_attachments') {
        this.evaluationSummary.set(await this.ideasApi.getEvaluations(id));
      }
    } catch {
      this.loadError.set($localize`:@@ideaDetailLoadError:Couldn't load this idea. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }

  async onSubmit(): Promise<void> {
    const current = this.idea();
    if (!current) return;
    this.errorMessage.set(null);
    try {
      await this.ideasApi.submit(current.id);
      this.idea.set(await this.ideasApi.getById(current.id));
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.queuedFiles.update((files) => [...files, ...Array.from(input.files!)]);
      input.value = '';
    }
  }

  removeQueuedFile(index: number): void {
    this.queuedFiles.update((files) => files.filter((_, i) => i !== index));
  }

  async onUploadQueuedFiles(): Promise<void> {
    const current = this.idea();
    if (!current) return;
    this.errorMessage.set(null);
    try {
      while (this.queuedFiles().length > 0) {
        const file = this.queuedFiles()[0];
        await this.ideasApi.uploadAttachment(current.id, file);
        this.queuedFiles.update((files) => files.slice(1));
      }
      this.idea.set(await this.ideasApi.getById(current.id));
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onSubmitToCommittee(): Promise<void> {
    const current = this.idea();
    if (!current) return;
    this.errorMessage.set(null);
    try {
      await this.committeeApi.submitToCommittee(current.id);
      this.idea.set(await this.ideasApi.getById(current.id));
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`Something went wrong. Please try again.`;
  }
}

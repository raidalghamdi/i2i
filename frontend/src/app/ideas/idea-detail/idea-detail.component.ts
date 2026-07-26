import { Component, LOCALE_ID, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EvaluationSummary, IdeasApiService } from '../ideas-api.service';
import { Idea, IdeaAttachment, IdeaJourney } from '../idea.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { StrategicThemesService } from '../strategic-themes.service';
import { ActivitiesService } from '../activities.service';
import { ChallengesService } from '../challenges.service';
import { IdeaHeroComponent } from '../idea-hero/idea-hero.component';
import { MeApiService } from '../../core/me-api.service';
import { IdentityService } from '../../core/auth/identity.service';
import { PostProgramStepperComponent } from '../post-program-stepper/post-program-stepper.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { StatusLabelPipe } from '../../shared/status-label/status-label.pipe';

const WITHDRAWABLE_STATUSES = new Set(['draft', 'submitted', 'returned']);
/** Statuses before the evaluator-review decision has been made — there is no feedback to show yet. */
const PRE_EVALUATION_DECISION_STATUSES = new Set(['draft', 'submitted', 'screening', 'returned', 'evaluation', 'withdrawn']);
/** How long to keep an attachment blob object-URL alive after opening it. */
const ATTACHMENT_URL_REVOKE_MS = 60_000;
/** Content types browsers can render inline in a tab; everything else is downloaded instead. */
const INLINE_VIEWABLE_TYPES = new Set(['application/pdf', 'image/png', 'image/jpeg', 'image/gif', 'image/webp']);

@Component({
  selector: 'app-idea-detail',
  imports: [
    RouterLink,
    FormsModule,
    PageHeaderComponent,
    StatusBadgeComponent,
    IdeaHeroComponent,
    PostProgramStepperComponent,
    LoadingStateComponent,
    ErrorStateComponent,
    DatePipe,
    StatusLabelPipe,
  ],
  templateUrl: './idea-detail.component.html',
})
export class IdeaDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly ideasApi = inject(IdeasApiService);
  private readonly themesApi = inject(StrategicThemesService);
  private readonly activitiesApi = inject(ActivitiesService);
  private readonly challengesApi = inject(ChallengesService);
  private readonly meApi = inject(MeApiService);
  private readonly identityService = inject(IdentityService);
  private readonly isArabic = inject(LOCALE_ID).toString().startsWith('ar');

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
  readonly resubmitComment = signal('');

  readonly isOwner = computed(() => {
    const idea = this.idea();
    return !!idea && this.currentUserId() === idea.submitterId;
  });

  /**
   * True when the signed-in user is a member of this idea's team (matched by AD SAM) but not
   * the owner — they can view the idea and its attachments, but must never see mutating controls
   * (edit, submit, attachment upload, submit-to-committee).
   */
  readonly isReadOnlyTeamMember = computed(() => {
    const idea = this.idea();
    if (!idea || this.isOwner()) return false;
    const sam = this.identityService.identity()?.samAccountName;
    if (!sam) return false;
    return idea.teamMembers.some(
      (member) => !!member.samAccountName && member.samAccountName.toLowerCase() === sam.toLowerCase(),
    );
  });

  readonly showPostProgram = computed(() => {
    const idea = this.idea();
    if (!idea || !this.isOwner()) return false;
    return ['approved', 'in_pilot', 'in_measurement', 'in_scaling'].includes(idea.status);
  });

  /** Owner-only withdraw action, available while the idea is still in a withdrawable status. */
  readonly canWithdraw = computed(() => {
    const idea = this.idea();
    return !!idea && this.isOwner() && WITHDRAWABLE_STATUSES.has(idea.status);
  });

  /** Evaluators must not see who is on the team, to keep evaluation unbiased. Judges see the
   * team via the committee decision screen, so they must NOT be hidden here. */
  readonly hideTeamRoster = computed(() => this.identityService.identity()?.activeRole === 'evaluator');

  /** Owner-only resubmit action, available while the idea is back with the submitter after the
   * evaluator-review decision (a supervisor "return"), pending a comment before it goes back. */
  readonly canResubmit = computed(() => {
    const idea = this.idea();
    return !!idea && this.isOwner() && idea.status === 'submitter_review';
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

      try {
        const me = await this.meApi.get();
        this.currentUserId.set(me.id);
      } catch {
        this.currentUserId.set(null);
      }

      try {
        this.journey.set(await this.ideasApi.getJourney(id));
      } catch {
        this.journey.set(null);
      }

      const [themes, activities] = await Promise.all([this.themesApi.list(), this.activitiesApi.list()]);
      const theme = themes.find((t) => t.id === idea.strategicThemeId);
      this.trackName.set((this.isArabic ? theme?.nameAr : theme?.nameEn) ?? null);
      const activity = activities.find((a) => a.id === idea.activityId);
      this.activityName.set((this.isArabic ? activity?.nameAr : activity?.nameEn) ?? null);
      if (idea.challengeId) {
        const challenges = await this.challengesApi.listByTheme(idea.strategicThemeId);
        const challenge = challenges.find((c) => c.id === idea.challengeId);
        this.challengeText.set((this.isArabic ? challenge?.textAr : challenge?.textEn) ?? null);
      }

      if (!PRE_EVALUATION_DECISION_STATUSES.has(idea.status)) {
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

  async openAttachment(attachment: IdeaAttachment): Promise<void> {
    const current = this.idea();
    if (!current) return;
    this.errorMessage.set(null);
    try {
      const blob = await this.ideasApi.getAttachmentBlob(current.id, attachment.id);
      // Give the blob its real content type so the browser knows how to handle it, then drive an
      // anchor click: inline-viewable types open in a new tab; everything else downloads by name.
      // (The previous window.open('','_blank') + location.href approach silently produced a blank
      // tab and never surfaced the file.)
      const typed = attachment.contentType ? blob.slice(0, blob.size, attachment.contentType) : blob;
      const url = URL.createObjectURL(typed);
      const anchor = document.createElement('a');
      anchor.href = url;
      if (INLINE_VIEWABLE_TYPES.has(attachment.contentType)) {
        anchor.target = '_blank';
        anchor.rel = 'noopener';
      } else {
        anchor.download = attachment.fileName;
      }
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      setTimeout(() => URL.revokeObjectURL(url), ATTACHMENT_URL_REVOKE_MS);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onResubmit(): Promise<void> {
    const current = this.idea();
    if (!current) return;
    const comment = this.resubmitComment().trim();
    if (!comment) return;
    this.errorMessage.set(null);
    try {
      await this.ideasApi.resubmitEvaluation(current.id, comment);
      this.resubmitComment.set('');
      this.idea.set(await this.ideasApi.getById(current.id));
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onWithdraw(): Promise<void> {
    const current = this.idea();
    if (!current) return;
    const confirmed = window.confirm(
      $localize`:@@ideaDetailWithdrawConfirm:Are you sure you want to withdraw this idea? This cannot be undone.`,
    );
    if (!confirmed) return;
    this.errorMessage.set(null);
    try {
      await this.ideasApi.withdraw(current.id);
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

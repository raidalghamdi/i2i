import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { EvaluationsApiService } from '../evaluations-api.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { IdeaContextPanelComponent } from '../../shared/idea-context-panel/idea-context-panel.component';
import { IdeasApiService } from '../../ideas/ideas-api.service';
import { StrategicThemesService } from '../../ideas/strategic-themes.service';
import { ActivitiesService } from '../../ideas/activities.service';
import { ChallengesService } from '../../ideas/challenges.service';
import { Idea, IdeaAttachment } from '../../ideas/idea.model';

/** How long to keep an attachment blob object-URL alive after opening it. */
const ATTACHMENT_URL_REVOKE_MS = 60_000;

@Component({
  selector: 'app-evaluation-form',
  imports: [ReactiveFormsModule, PageHeaderComponent, IdeaContextPanelComponent],
  templateUrl: './evaluation-form.component.html',
})
export class EvaluationFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly evaluationsApi = inject(EvaluationsApiService);
  private readonly ideasApi = inject(IdeasApiService);
  private readonly themesApi = inject(StrategicThemesService);
  private readonly activitiesApi = inject(ActivitiesService);
  private readonly challengesApi = inject(ChallengesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly errorMessage = signal<string | null>(null);
  private readonly ideaId = this.route.snapshot.paramMap.get('id')!;

  /** 0..10 integer options for the score dropdowns. */
  readonly scoreOptions = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

  /** The idea being evaluated, shown beside the form so the evaluator isn't scoring blind. */
  readonly idea = signal<Idea | null>(null);
  /** Set when the idea (or its classification names) couldn't be fetched; the form still shows. */
  readonly ideaInfoUnavailable = signal(false);
  readonly trackName = signal<string | null>(null);
  readonly activityName = signal<string | null>(null);
  readonly challengeText = signal<string | null>(null);

  readonly form = this.fb.group({
    innovation: this.fb.control<number | null>(null, [Validators.required, Validators.min(0), Validators.max(10)]),
    impact: this.fb.control<number | null>(null, [Validators.required, Validators.min(0), Validators.max(10)]),
    execution: this.fb.control<number | null>(null, [Validators.required, Validators.min(0), Validators.max(10)]),
    scalability: this.fb.control<number | null>(null, [Validators.required, Validators.min(0), Validators.max(10)]),
    presentation: this.fb.control<number | null>(null, [Validators.required, Validators.min(0), Validators.max(10)]),
    comments: this.fb.control<string | null>(null),
  });

  async ngOnInit(): Promise<void> {
    await this.loadIdea();
  }

  /**
   * Loads the idea being evaluated and resolves the Arabic names of its track/activity/challenge,
   * mirroring idea-detail.component.ts's name-resolution pattern. Deliberately does NOT touch
   * idea.teamMembers — evaluators must not see who's on the team (unbiased evaluation).
   */
  private async loadIdea(): Promise<void> {
    this.ideaInfoUnavailable.set(false);
    try {
      const idea = await this.ideasApi.getById(this.ideaId);
      this.idea.set(idea);

      const [themes, activities] = await Promise.all([this.themesApi.list(), this.activitiesApi.list()]);
      const theme = themes.find((t) => t.id === idea.strategicThemeId);
      this.trackName.set(theme?.nameAr ?? null);
      const activity = activities.find((a) => a.id === idea.activityId);
      this.activityName.set(activity?.nameAr ?? null);
      if (idea.challengeId) {
        const challenges = await this.challengesApi.listByTheme(idea.strategicThemeId);
        const challenge = challenges.find((c) => c.id === idea.challengeId);
        this.challengeText.set(challenge?.textAr ?? null);
      }
    } catch {
      this.idea.set(null);
      this.ideaInfoUnavailable.set(true);
    }
  }

  async openAttachment(attachment: IdeaAttachment): Promise<void> {
    const current = this.idea();
    if (!current) return;
    this.errorMessage.set(null);
    // Open the tab synchronously inside the click gesture so popup blockers don't block it;
    // navigate it to the blob once it resolves (fall back to a download if the popup was blocked).
    const win = window.open('', '_blank');
    try {
      const blob = await this.ideasApi.getAttachmentBlob(current.id, attachment.id);
      const url = URL.createObjectURL(blob);
      if (win) {
        win.location.href = url;
      } else {
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = attachment.fileName;
        anchor.click();
      }
      setTimeout(() => URL.revokeObjectURL(url), ATTACHMENT_URL_REVOKE_MS);
    } catch (error) {
      win?.close();
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);

    const value = this.form.getRawValue();
    try {
      await this.evaluationsApi.submit(this.ideaId, {
        innovation: value.innovation!,
        impact: value.impact!,
        execution: value.execution!,
        scalability: value.scalability!,
        presentation: value.presentation!,
        comments: value.comments,
      });
      await this.router.navigate(['/evaluations/queue']);
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

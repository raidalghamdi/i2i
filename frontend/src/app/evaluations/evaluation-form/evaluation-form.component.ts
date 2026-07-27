import { Component, Inject, LOCALE_ID, OnInit, inject, signal } from '@angular/core'; // Change 20260726
import { FormArray, FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms'; // Change 20260726
import { ActivatedRoute, Router } from '@angular/router';
import { EvaluationsApiService } from '../evaluations-api.service';
import { EvaluationAction, EvaluationCriterion } from '../evaluation.model'; // Change 20260726
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

  /** Admin-configurable criteria, in sortOrder. Parallel to the `scores` FormArray by index. */
  readonly criteria = signal<EvaluationCriterion[]>([]); // Change 20260726
  /** Set when the criteria fetch failed — without them there is nothing valid to submit. */
  readonly criteriaUnavailable = signal(false); // Change 20260726
  /** True when this form was populated from a previously saved draft. */
  readonly resumedDraft = signal(false); // Change 20260726
  readonly isSubmitting = signal(false); // Change 20260726

  private readonly isArabic: boolean; // Change 20260726

  // Change 20260726 — one control per criterion, appended once the criteria arrive.
  readonly form = this.fb.group({
    scores: this.fb.array<FormControl<number | null>>([]),
    comments: this.fb.control<string | null>(null),
    conflictOfInterest: this.fb.nonNullable.control(false),
  });

  // Change 20260726
  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  // Change 20260726
  get scores(): FormArray<FormControl<number | null>> {
    return this.form.controls.scores;
  }

  // Change 20260726
  criterionName(criterion: EvaluationCriterion): string {
    return this.isArabic ? criterion.nameAr : criterion.nameEn;
  }

  // Change 20260726
  criterionDescription(criterion: EvaluationCriterion): string | null {
    return this.isArabic ? criterion.descriptionAr : criterion.descriptionEn;
  }

  /** Weights are stored as fractions of 1.0; evaluators read them as percentages. */
  // Change 20260726
  weightPercent(criterion: EvaluationCriterion): number {
    return Math.round(criterion.weight * 100);
  }

  async ngOnInit(): Promise<void> {
    await this.loadCriteria(); // Change 20260726
    await this.loadIdea();
    await this.loadDraft(); // Change 20260726
  }

  /**
   * Builds the score controls from the server-side criteria list. The criteria are
   * admin-configurable, so neither their number nor their codes can be assumed here.
   */
  // Change 20260726
  private async loadCriteria(): Promise<void> {
    this.criteriaUnavailable.set(false);
    try {
      const criteria = await this.evaluationsApi.getCriteria();
      this.criteria.set(criteria);
      this.scores.clear();
      for (const _ of criteria) {
        this.scores.push(this.fb.control<number | null>(null, [Validators.required, Validators.min(0), Validators.max(10)]));
      }
    } catch {
      this.criteria.set([]);
      this.scores.clear();
      this.criteriaUnavailable.set(true);
      this.errorMessage.set($localize`:@@evalFormCriteriaLoadError:Couldn't load the scoring criteria. Please reload the page.`);
    }
  }

  /**
   * Restores an unsubmitted draft for this idea, if one exists, so a partially scored
   * evaluation can be resumed rather than restarted.
   */
  // Change 20260726
  private async loadDraft(): Promise<void> {
    if (this.criteria().length === 0) return;
    try {
      const mine = await this.evaluationsApi.getMine();
      const draft = mine.find((e) => e.ideaId === this.ideaId && e.submittedAt === null);
      if (!draft) return;

      const saved = JSON.parse(draft.criteriaScoresJson ?? '{}') as Record<string, number>;
      this.criteria().forEach((criterion, index) => {
        const score = saved[criterion.code];
        if (typeof score === 'number') this.scores.at(index).setValue(score);
      });
      this.form.controls.comments.setValue(draft.comments ?? null);
      this.form.controls.conflictOfInterest.setValue(draft.conflictOfInterest ?? false);
      this.resumedDraft.set(true);
    } catch {
      // A draft that can't be read must not block a fresh evaluation.
    }
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

  /** Collects the filled-in scores keyed by criterion code; drafts may be partial. */
  // Change 20260726
  private collectScores(): Record<string, number> {
    const scores: Record<string, number> = {};
    this.criteria().forEach((criterion, index) => {
      const value = this.scores.at(index)?.value;
      if (value !== null && value !== undefined) scores[criterion.code] = value;
    });
    return scores;
  }

  async onSubmit(): Promise<void> {
    if (this.criteriaUnavailable() || this.form.invalid || this.isSubmitting()) { // Change 20260726
      this.form.markAllAsTouched();
      return;
    }
    await this.send('submit'); // Change 20260726
  }

  /** Saves progress without validating: a draft is allowed to be incomplete. */
  // Change 20260726
  async onSaveDraft(): Promise<void> {
    if (this.criteriaUnavailable() || this.isSubmitting()) return; // Change 20260726
    await this.send('draft');
  }

  // Change 20260726
  private async send(action: EvaluationAction): Promise<void> {
    this.errorMessage.set(null);
    this.isSubmitting.set(true); // Change 20260726
    const value = this.form.getRawValue();
    try {
      await this.evaluationsApi.submit(this.ideaId, {
        criteriaScores: this.collectScores(),
        comments: value.comments,
        action,
        conflictOfInterest: value.conflictOfInterest,
      });
      await this.router.navigate([action === 'draft' ? '/evaluations/mine' : '/evaluations/queue']);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.isSubmitting.set(false); // Change 20260726
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

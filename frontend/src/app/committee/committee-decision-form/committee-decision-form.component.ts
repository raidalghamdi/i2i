import { Component, OnInit, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { IdeaContextPanelComponent } from '../../shared/idea-context-panel/idea-context-panel.component';
import { CommitteeApiService } from '../committee-api.service';
import { CommitteeCriterion } from '../committee.model';
import { IdeasApiService } from '../../ideas/ideas-api.service';
import { Idea } from '../../ideas/idea.model';
import { EvaluationReviewApiService } from '../../evaluations/evaluation-review-api.service';
import { EvaluationReviewDetail } from '../../evaluations/evaluation-review.model';

@Component({
  selector: 'app-committee-decision-form',
  imports: [ReactiveFormsModule, PageHeaderComponent, LoadingStateComponent, ErrorStateComponent, IdeaContextPanelComponent],
  templateUrl: './committee-decision-form.component.html',
})
export class CommitteeDecisionFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly committeeApi = inject(CommitteeApiService);
  private readonly ideasApi = inject(IdeasApiService);
  private readonly evaluationReviewApi = inject(EvaluationReviewApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly criteria = signal<CommitteeCriterion[]>([]);
  readonly idea = signal<Idea | null>(null);
  readonly detail = signal<EvaluationReviewDetail | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  private readonly ideaId = this.route.snapshot.paramMap.get('id')!;

  readonly form = this.fb.group({
    decisionTypeCode: this.fb.control<string>('', Validators.required),
    comments: this.fb.control<string | null>(null),
  });

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  reload(): void {
    void this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const [criteria, idea, detail] = await Promise.all([
        this.committeeApi.getCriteria(),
        this.ideasApi.getById(this.ideaId),
        this.evaluationReviewApi.getDetail(this.ideaId),
      ]);
      // `this.form` is statically typed to only know about `decisionTypeCode`/`comments` (the controls
      // declared in the `fb.group({...})` call above), so Angular 22's typed-forms `addControl` overloads
      // reject a dynamic `criterion.code` string. Widen to the untyped `FormGroup<Record<string, AbstractControl>>`
      // shape (the same shape `addControl`'s first overload expects) purely for this call; the rest of the
      // component keeps using the strongly-typed `this.form`.
      const untypedForm = this.form as unknown as FormGroup<Record<string, AbstractControl>>;
      for (const criterion of criteria) {
        if (untypedForm.contains(criterion.code)) continue;
        untypedForm.addControl(
          criterion.code,
          this.fb.control<number | null>(null, [Validators.required, Validators.min(0), Validators.max(10)]),
        );
      }
      this.criteria.set(criteria);
      this.idea.set(idea);
      this.detail.set(detail);
    } catch {
      this.loadError.set($localize`:@@committeeDecisionFormLoadError:Couldn't load the decision criteria. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }

  async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);

    const criteriaScores: Record<string, number> = {};
    for (const criterion of this.criteria()) {
      criteriaScores[criterion.code] = this.form.get(criterion.code)!.value as number;
    }

    try {
      await this.committeeApi.submitDecision(this.ideaId, {
        decisionTypeCode: this.form.get('decisionTypeCode')!.value as string,
        criteriaScores,
        comments: (this.form.get('comments')?.value as string | null) ?? null,
      });
      await this.router.navigate(['/committee/queue']);
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

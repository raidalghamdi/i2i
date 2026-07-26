import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { IdeasApiService } from '../../ideas/ideas-api.service';
import { Idea } from '../../ideas/idea.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { IdeaContextPanelComponent } from '../../shared/idea-context-panel/idea-context-panel.component';
import { SectionMultiselectComponent } from '../../shared/section-multiselect/section-multiselect.component';
import { EvaluationReviewApiService } from '../../evaluations/evaluation-review-api.service';
import { EvaluationReviewDecisionInput, EvaluationReviewDetail } from '../../evaluations/evaluation-review.model';

function reasonRequiredForReturn(): ValidatorFn {
  return (group): ValidationErrors | null => {
    const decisionCode = group.get('decisionCode')?.value as string;
    const reason = (group.get('reason')?.value as string | null) ?? '';
    if (decisionCode === 'return' && reason.trim().length < 10) return { reasonRequired: true };
    return null;
  };
}

@Component({
  selector: 'app-evaluation-review-form',
  imports: [
    ReactiveFormsModule,
    PageHeaderComponent,
    LoadingStateComponent,
    ErrorStateComponent,
    IdeaContextPanelComponent,
    SectionMultiselectComponent,
  ],
  templateUrl: './evaluation-review-form.component.html',
})
export class EvaluationReviewFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ideasApi = inject(IdeasApiService);
  private readonly evaluationReviewApi = inject(EvaluationReviewApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly idea = signal<Idea | null>(null);
  readonly detail = signal<EvaluationReviewDetail | null>(null);
  readonly selectedSections = signal<string[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  private readonly ideaId = this.route.snapshot.paramMap.get('id')!;

  readonly form = this.fb.group(
    {
      decisionCode: this.fb.control<string>('', Validators.required),
      supervisorComment: this.fb.control<string>(''),
      reason: this.fb.control<string>(''),
    },
    { validators: reasonRequiredForReturn() },
  );

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
      const [idea, detail] = await Promise.all([
        this.ideasApi.getById(this.ideaId),
        this.evaluationReviewApi.getDetail(this.ideaId),
      ]);
      this.idea.set(idea);
      this.detail.set(detail);
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(
          error,
          $localize`:@@evaluationReviewFormLoadError:Couldn't load this idea. Please try again.`,
        ),
      );
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

    const decisionCode = this.form.get('decisionCode')!.value as 'forward' | 'return' | 'fail';
    const input: EvaluationReviewDecisionInput = { decisionCode };

    if (decisionCode === 'forward') {
      const supervisorComment = this.form.get('supervisorComment')!.value as string;
      input.supervisorComment = supervisorComment.trim().length > 0 ? supervisorComment : null;
    } else if (decisionCode === 'return') {
      input.reason = this.form.get('reason')!.value as string;
      input.editableSections = this.selectedSections();
    } else if (decisionCode === 'fail') {
      const reason = this.form.get('reason')!.value as string;
      input.reason = reason.trim().length > 0 ? reason : null;
    }

    try {
      await this.evaluationReviewApi.submitDecision(this.ideaId, input);
      await this.router.navigate(['/supervisor/evaluation-review']);
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

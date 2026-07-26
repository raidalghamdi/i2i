import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { IdeasApiService } from '../../ideas/ideas-api.service';
import { Idea } from '../../ideas/idea.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { IdeaContextPanelComponent } from '../../shared/idea-context-panel/idea-context-panel.component';
import { RoleUserMultiselectComponent } from '../../shared/role-user-multiselect/role-user-multiselect.component';
import { SectionMultiselectComponent } from '../../shared/section-multiselect/section-multiselect.component';
import { SupervisorApiService } from '../supervisor-api.service';
import { RoleUser, ScreeningDecisionInput } from '../supervisor.model';

function reasonRequiredForRejectOrReturn(): ValidatorFn {
  return (group): ValidationErrors | null => {
    const decisionCode = group.get('decisionCode')?.value as string;
    const reason = (group.get('reason')?.value as string | null) ?? '';
    if (decisionCode === 'reject' && reason.trim().length === 0) return { reasonRequired: true };
    if (decisionCode === 'return' && reason.trim().length < 10) return { reasonRequired: true };
    return null;
  };
}

@Component({
  selector: 'app-screening-decision-form',
  imports: [
    ReactiveFormsModule,
    PageHeaderComponent,
    LoadingStateComponent,
    ErrorStateComponent,
    IdeaContextPanelComponent,
    RoleUserMultiselectComponent,
    SectionMultiselectComponent,
  ],
  templateUrl: './screening-decision-form.component.html',
})
export class ScreeningDecisionFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ideasApi = inject(IdeasApiService);
  private readonly supervisorApi = inject(SupervisorApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly idea = signal<Idea | null>(null);
  readonly evaluators = signal<RoleUser[]>([]);
  readonly selectedEvaluatorIds = signal<string[]>([]);
  readonly selectedSections = signal<string[]>([]);
  readonly evaluatorSelectionError = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  private readonly ideaId = this.route.snapshot.paramMap.get('id')!;

  readonly form = this.fb.group(
    {
      decisionCode: this.fb.control<string>('', Validators.required),
      reason: this.fb.control<string>(''),
    },
    { validators: reasonRequiredForRejectOrReturn() },
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
      const [idea, evaluators] = await Promise.all([
        this.ideasApi.getById(this.ideaId),
        this.supervisorApi.getUsersByRole('evaluator'),
      ]);
      this.idea.set(idea);
      this.evaluators.set(evaluators);
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(
          error,
          $localize`:@@screeningDecisionFormLoadError:Couldn't load this idea. Please try again.`,
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

    const decisionCode = this.form.get('decisionCode')!.value as string;
    this.evaluatorSelectionError.set(null);
    if (decisionCode === 'approve' && this.selectedEvaluatorIds().length === 0) {
      this.evaluatorSelectionError.set(
        $localize`:@@screeningFormEvaluatorRequired:Select at least one evaluator before approving.`,
      );
      return;
    }

    this.errorMessage.set(null);

    const reason = this.form.get('reason')!.value as string;
    const input: ScreeningDecisionInput = {
      decisionCode,
      reason: reason.trim().length > 0 ? reason : null,
    };
    if (decisionCode === 'approve') {
      input.evaluatorIds = this.selectedEvaluatorIds();
    } else if (decisionCode === 'return') {
      input.editableSections = this.selectedSections();
    }

    try {
      await this.supervisorApi.submitScreeningDecision(this.ideaId, input);
      await this.router.navigate(['/supervisor/screening']);
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

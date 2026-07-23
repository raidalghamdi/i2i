import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { EvaluationsApiService } from '../evaluations-api.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';

@Component({
  selector: 'app-evaluation-form',
  imports: [ReactiveFormsModule, PageHeaderComponent],
  templateUrl: './evaluation-form.component.html',
})
export class EvaluationFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly evaluationsApi = inject(EvaluationsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly errorMessage = signal<string | null>(null);
  private readonly ideaId = this.route.snapshot.paramMap.get('id')!;

  readonly form = this.fb.group({
    innovation: this.fb.control<number | null>(null, [Validators.required, Validators.min(0), Validators.max(10)]),
    impact: this.fb.control<number | null>(null, [Validators.required, Validators.min(0), Validators.max(10)]),
    execution: this.fb.control<number | null>(null, [Validators.required, Validators.min(0), Validators.max(10)]),
    scalability: this.fb.control<number | null>(null, [Validators.required, Validators.min(0), Validators.max(10)]),
    presentation: this.fb.control<number | null>(null, [Validators.required, Validators.min(0), Validators.max(10)]),
    comments: this.fb.control<string | null>(null),
  });

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

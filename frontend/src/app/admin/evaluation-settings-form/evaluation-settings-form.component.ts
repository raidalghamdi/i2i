import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { EvaluationSettingsApiService } from '../evaluation-settings-api.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';

@Component({
  selector: 'app-evaluation-settings-form',
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './evaluation-settings-form.component.html',
})
export class EvaluationSettingsFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(EvaluationSettingsApiService);

  readonly saved = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly updatedAt = signal<string | null>(null);
  readonly loading = signal(true);
  /** Holds the initial-load failure message (separate from `errorMessage`, which is save-action-only). */
  readonly loadError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    passThreshold: [6, [Validators.required, Validators.min(0), Validators.max(10)]],
  });

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.loadError.set(null);
    this.loading.set(true);
    try {
      const settings = await this.api.get();
      this.form.patchValue({ passThreshold: settings.passThreshold });
      this.updatedAt.set(settings.updatedAt);
    } catch (error) {
      this.loadError.set(this.extractErrorMessage(error));
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
    this.saved.set(false);
    try {
      const updated = await this.api.update(this.form.getRawValue());
      this.form.patchValue({ passThreshold: updated.passThreshold });
      this.updatedAt.set(updated.updatedAt);
      this.saved.set(true);
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

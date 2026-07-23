import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InvitationSettingsApiService } from '../invitation-settings-api.service';
import { RosterApiService } from '../roster-api.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';

@Component({
  selector: 'app-invitation-settings-form',
  imports: [ReactiveFormsModule, PageHeaderComponent, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './invitation-settings-form.component.html',
})
export class InvitationSettingsFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(InvitationSettingsApiService);
  private readonly rosterApi = inject(RosterApiService);

  readonly saved = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    enabled: [true],
    cronExpression: ['', Validators.required],
    timezone: ['', Validators.required],
    stopAfterNReminders: [0, [Validators.required, Validators.min(0)]],
    gapHours: [0, [Validators.required, Validators.min(1)]],
    expiresDays: [0, [Validators.required, Validators.min(1)]],
    fromName: ['', Validators.required],
    fromEmail: ['', [Validators.required, Validators.email]],
    programNameAr: ['', Validators.required],
    programNameEn: ['', Validators.required],
  });

  readonly roleInvitationSaved = signal(false);
  readonly roleInvitationErrorMessage = signal<string | null>(null);

  readonly roleInvitationForm = this.fb.nonNullable.group({
    enabled: [true],
    defaultExpiresDays: [0, [Validators.required, Validators.min(1)]],
    reminderGapHours: [0, [Validators.required, Validators.min(1)]],
    maxReminders: [0, [Validators.required, Validators.min(0)]],
  });

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
      const [settings, roleInvitationSettings] = await Promise.all([this.api.get(), this.rosterApi.getSettings()]);
      this.form.patchValue(settings);
      this.roleInvitationForm.patchValue(roleInvitationSettings);
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@invitationSettingsLoadError:Couldn't load invitation settings. Please try again.`),
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
    this.saved.set(false);
    try {
      const updated = await this.api.update(this.form.getRawValue());
      this.form.patchValue(updated);
      this.saved.set(true);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onSubmitRoleInvitationSettings(): Promise<void> {
    if (this.roleInvitationForm.invalid) {
      this.roleInvitationForm.markAllAsTouched();
      return;
    }
    this.roleInvitationErrorMessage.set(null);
    this.roleInvitationSaved.set(false);
    try {
      const updated = await this.rosterApi.updateSettings(this.roleInvitationForm.getRawValue());
      this.roleInvitationForm.patchValue(updated);
      this.roleInvitationSaved.set(true);
    } catch (error) {
      this.roleInvitationErrorMessage.set(this.extractErrorMessage(error));
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

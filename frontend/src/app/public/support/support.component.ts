import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';
import { SupportApiService } from '../../core/support-api.service';

@Component({
  selector: 'app-support',
  imports: [PublicPageHeroComponent, ReactiveFormsModule],
  templateUrl: './support.component.html',
})
export class SupportComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(SupportApiService);

  readonly pageTitle = $localize`:@@supportTitle:Support`;
  readonly pageBody = $localize`:@@supportBody:Get in touch with the innovation team.`;

  readonly contactEmail = 'innovation@gac.gov.sa';
  readonly contactHours = $localize`:@@supportHours:Sunday–Thursday, 9:00–16:00`;
  readonly contactAddress = $localize`:@@supportAddress:General Authority for Competition, Riyadh, Kingdom of Saudi Arabia`;

  readonly sent = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: [''],
    email: ['', [Validators.required, Validators.email]],
    subject: [''],
    message: ['', [Validators.required]],
  });

  async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);
    this.sent.set(false);
    try {
      await this.api.submit(this.form.getRawValue());
      this.sent.set(true);
      this.form.reset();
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

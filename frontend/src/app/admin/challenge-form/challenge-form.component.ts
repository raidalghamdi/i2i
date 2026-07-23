import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ChallengeApiService } from '../challenge-api.service';
import { StrategicThemesService } from '../../ideas/strategic-themes.service';
import { StrategicTheme } from '../../ideas/idea.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';

@Component({
  selector: 'app-challenge-form',
  imports: [ReactiveFormsModule, PageHeaderComponent, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './challenge-form.component.html',
})
export class ChallengeFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly challengeApi = inject(ChallengeApiService);
  private readonly themesApi = inject(StrategicThemesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly themes = signal<StrategicTheme[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  private challengeId: string | null = null;

  readonly form = this.fb.nonNullable.group({
    strategicThemeId: ['', Validators.required],
    textAr: ['', Validators.required],
    textEn: ['', Validators.required],
    sortOrder: [0, Validators.required],
    isActive: [true],
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
      this.themes.set(await this.themesApi.list());

      const id = this.route.snapshot.paramMap.get('id');
      if (id) {
        this.challengeId = id;
        const challenge = await this.challengeApi.getById(id);
        this.form.patchValue({
          strategicThemeId: challenge.strategicThemeId,
          textAr: challenge.textAr,
          textEn: challenge.textEn,
          sortOrder: challenge.sortOrder,
          isActive: challenge.isActive,
        });
      }
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@challengeFormLoadError:Couldn't load this challenge. Please try again.`),
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
    const input = this.form.getRawValue();

    try {
      if (this.challengeId) {
        await this.challengeApi.update(this.challengeId, input);
      } else {
        await this.challengeApi.create(input);
      }
      await this.router.navigate(['/admin/challenges']);
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

import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CmsApiService } from '../cms-api.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';

@Component({
  selector: 'app-content-string-form',
  imports: [ReactiveFormsModule, PageHeaderComponent, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './content-string-form.component.html',
})
export class ContentStringFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cmsApi = inject(CmsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  private stringId: string | null = null;

  readonly form = this.fb.nonNullable.group({
    key: ['', Validators.required],
    valueAr: ['', Validators.required],
    valueEn: ['', Validators.required],
  });

  ngOnInit(): Promise<void> {
    return this.load();
  }

  reload(): Promise<void> {
    return this.load();
  }

  private async load(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.loadError.set(null);
    try {
      this.stringId = id;
      const contentString = await this.cmsApi.getString(id);
      this.form.patchValue({
        key: contentString.key,
        valueAr: contentString.valueAr,
        valueEn: contentString.valueEn,
      });
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@contentStringFormLoadError:Couldn't load this content string. Please try again.`),
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
      if (this.stringId) {
        await this.cmsApi.updateString(this.stringId, input);
      } else {
        await this.cmsApi.createString(input);
      }
      await this.router.navigate(['/admin/cms/strings']);
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

import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CmsApiService } from '../cms-api.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';

@Component({
  selector: 'app-cms-content-form',
  imports: [ReactiveFormsModule, PageHeaderComponent, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './cms-content-form.component.html',
})
export class CmsContentFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cmsApi = inject(CmsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  private contentId: string | null = null;

  readonly form = this.fb.nonNullable.group({
    slug: ['', Validators.required],
    titleAr: ['', Validators.required],
    titleEn: ['', Validators.required],
    bodyAr: ['', Validators.required],
    bodyEn: ['', Validators.required],
    isPublished: [true],
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
      this.contentId = id;
      const page = await this.cmsApi.getContent(id);
      this.form.patchValue({
        slug: page.slug,
        titleAr: page.titleAr,
        titleEn: page.titleEn,
        bodyAr: page.bodyAr,
        bodyEn: page.bodyEn,
        isPublished: page.isPublished,
      });
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@cmsContentFormLoadError:Couldn't load this page. Please try again.`),
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
      if (this.contentId) {
        await this.cmsApi.updateContent(this.contentId, input);
      } else {
        await this.cmsApi.createContent(input);
      }
      await this.router.navigate(['/admin/cms/content']);
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

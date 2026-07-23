import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CmsApiService } from '../cms-api.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';

@Component({
  selector: 'app-cms-block-form',
  imports: [ReactiveFormsModule, PageHeaderComponent, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './cms-block-form.component.html',
})
export class CmsBlockFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cmsApi = inject(CmsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  private blockId: string | null = null;

  readonly form = this.fb.nonNullable.group({
    key: ['', Validators.required],
    contentAr: ['', Validators.required],
    contentEn: ['', Validators.required],
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
      this.blockId = id;
      const block = await this.cmsApi.getBlock(id);
      this.form.patchValue({
        key: block.key,
        contentAr: block.contentAr,
        contentEn: block.contentEn,
        isPublished: block.isPublished,
      });
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@cmsBlockFormLoadError:Couldn't load this content block. Please try again.`),
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
      if (this.blockId) {
        await this.cmsApi.updateBlock(this.blockId, input);
      } else {
        await this.cmsApi.createBlock(input);
      }
      await this.router.navigate(['/admin/cms/blocks']);
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

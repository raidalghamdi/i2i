import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { AdminApiService } from '../admin-api.service';
import { IdeaTemplateInfo } from '../admin.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';

@Component({
  selector: 'app-idea-template',
  imports: [DatePipe, PageHeaderComponent, LoadingStateComponent],
  templateUrl: './idea-template.component.html',
})
export class IdeaTemplateComponent implements OnInit {
  private readonly api = inject(AdminApiService);

  readonly current = signal<IdeaTemplateInfo | null>(null);
  readonly loading = signal(true);
  readonly uploading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  ngOnInit(): Promise<void> {
    return this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      this.current.set(await this.api.getCurrentIdeaTemplate());
    } finally {
      this.loading.set(false);
    }
  }

  async onFileSelected(file: File): Promise<void> {
    this.uploading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    try {
      const info = await this.api.uploadIdeaTemplate(file);
      this.current.set(info);
      this.successMessage.set(
        $localize`:@@adminIdeaTemplateUploadSuccess:Template updated successfully.`,
      );
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.uploading.set(false);
    }
  }

  formatSize(sizeBytes: number): string {
    if (sizeBytes < 1024) return `${sizeBytes} B`;
    return `${(sizeBytes / 1024).toFixed(1)} KB`;
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@adminIdeaTemplateUploadError:Couldn't upload the template. Please try again.`;
  }
}

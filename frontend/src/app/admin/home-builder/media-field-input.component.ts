import { Component, inject, input, output, signal } from '@angular/core';
import { HomeBuilderApiService } from './home-builder-api.service';

/** Reusable upload/preview/remove control for a single image or video field, backed directly
 * by `POST /api/admin/home/media`. Used across the homepage-section editor wherever a field's
 * `kind` is `image`/`video` (top-level scalar fields as well as `objArray` sub-fields), so the
 * upload/uploading/error state lives once per control instance instead of being hand-rolled and
 * key-tracked by the parent for each of the three use-sites. */
@Component({
  selector: 'app-media-field-input',
  templateUrl: './media-field-input.component.html',
})
export class MediaFieldInputComponent {
  private readonly api = inject(HomeBuilderApiService);

  readonly label = input<string>();
  readonly value = input.required<string>();
  readonly kind = input.required<'image' | 'video'>();
  readonly valueChange = output<string>();

  readonly uploading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  protected readonly uploadLabel = $localize`:@@homeBuilderMediaUploadLabel:Upload`;
  protected readonly uploadingLabel = $localize`:@@homeBuilderMediaUploadingLabel:Uploading…`;
  protected readonly removeLabel = $localize`:@@homeBuilderMediaRemoveButton:Remove`;

  async onFileSelected(file: File | undefined | null): Promise<void> {
    if (!file) return;
    this.uploading.set(true);
    this.errorMessage.set(null);
    try {
      const result = await this.api.uploadMedia(file);
      this.valueChange.emit(result.url);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.uploading.set(false);
    }
  }

  remove(): void {
    this.valueChange.emit('');
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@homeBuilderMediaUploadError:Couldn't upload the file. Please try again.`;
  }
}

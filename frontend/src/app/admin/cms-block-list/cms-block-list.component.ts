import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CmsApiService } from '../cms-api.service';
import { CmsBlock } from '../cms.model';
import { IconComponent } from '../../shared/icon/icon.component';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';

@Component({
  selector: 'app-cms-block-list',
  imports: [RouterLink, IconComponent, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './cms-block-list.component.html',
})
export class CmsBlockListComponent implements OnInit {
  private readonly cmsApi = inject(CmsApiService);

  readonly blocks = signal<CmsBlock[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);

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
      this.blocks.set(await this.cmsApi.listBlocks());
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@cmsBlockListLoadError:Couldn't load content blocks. Please try again.`),
      );
    } finally {
      this.loading.set(false);
    }
  }

  async onDelete(id: string): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.cmsApi.deleteBlock(id);
      this.blocks.set(await this.cmsApi.listBlocks());
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

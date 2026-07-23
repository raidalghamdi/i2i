import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { CommitteeApiService } from '../committee-api.service';
import { CommitteeQueueItem } from '../committee.model';

@Component({
  selector: 'app-committee-queue',
  imports: [RouterLink, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './committee-queue.component.html',
})
export class CommitteeQueueComponent implements OnInit {
  private readonly committeeApi = inject(CommitteeApiService);
  readonly queue = signal<CommitteeQueueItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  reload(): void {
    void this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.queue.set(await this.committeeApi.getQueue());
    } catch {
      this.error.set($localize`:@@committeeQueueLoadError:Couldn't load the committee queue. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }
}

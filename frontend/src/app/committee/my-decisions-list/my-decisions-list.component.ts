import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { CommitteeApiService } from '../committee-api.service';
import { MyCommitteeDecision } from '../committee.model';

@Component({
  selector: 'app-my-decisions-list',
  imports: [DatePipe, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './my-decisions-list.component.html',
})
export class MyDecisionsListComponent implements OnInit {
  private readonly committeeApi = inject(CommitteeApiService);
  readonly decisions = signal<MyCommitteeDecision[]>([]);
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
      this.decisions.set(await this.committeeApi.getMine());
    } catch {
      this.error.set($localize`:@@myDecisionsLoadError:Couldn't load your committee decisions. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }
}

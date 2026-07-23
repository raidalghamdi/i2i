import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RoleKpiCardComponent } from '../../shared/role-kpi-card/role-kpi-card.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { DashboardApiService } from '../../core/dashboard-api.service';
import { CommitteeDashboard } from '../../core/dashboard.model';

@Component({
  selector: 'app-committee-dashboard',
  imports: [RouterLink, RoleKpiCardComponent, LoadingStateComponent],
  templateUrl: './committee-dashboard.component.html',
})
export class CommitteeDashboardComponent implements OnInit {
  private readonly api = inject(DashboardApiService);
  readonly data = signal<CommitteeDashboard | null>(null);
  readonly loading = signal(true);
  readonly awaitingLabel = $localize`:@@committeeDashAwaiting:Ideas Awaiting Decision`;
  readonly decisionsLabel = $localize`:@@committeeDashDecisions:Decisions This Week`;

  async ngOnInit(): Promise<void> {
    try {
      this.data.set(await this.api.getCommittee());
    } catch {
      // Degrade gracefully to zeros rather than blocking this navigation hub
      // with an error state.
      this.data.set({ awaitingDecision: 0, decisionsThisWeek: 0 });
    } finally {
      this.loading.set(false);
    }
  }
}

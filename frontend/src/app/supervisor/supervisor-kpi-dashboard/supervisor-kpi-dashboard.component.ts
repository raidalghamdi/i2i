import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RoleKpiCardComponent } from '../../shared/role-kpi-card/role-kpi-card.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { DashboardApiService } from '../../core/dashboard-api.service';
import { SupervisorDashboard } from '../../core/dashboard.model';

@Component({
  selector: 'app-supervisor-kpi-dashboard',
  imports: [RouterLink, RoleKpiCardComponent, LoadingStateComponent],
  templateUrl: './supervisor-kpi-dashboard.component.html',
})
export class SupervisorKpiDashboardComponent implements OnInit {
  private readonly api = inject(DashboardApiService);
  readonly data = signal<SupervisorDashboard | null>(null);
  readonly loading = signal(true);
  readonly teamLabel = $localize`:@@supDashTeam:Team Members`;
  readonly sectorLabel = $localize`:@@supDashSector:Ideas From My Sector`;
  readonly escalationsLabel = $localize`:@@supDashEscalations:Escalations Awaiting Me`;

  async ngOnInit(): Promise<void> {
    try {
      this.data.set(await this.api.getSupervisor());
    } catch {
      // Supervisor KPIs degrade gracefully to zeros rather than blocking the
      // page with an error — this is a navigation hub, not a single-purpose
      // data view, so it should stay usable even if the KPI fetch fails.
      this.data.set({
        teamMembers: 0,
        sectorIdeas: 0,
        escalationsAwaitingMe: 0,
        screening: { total: 0, underReview: 0, approved: 0, returned: 0, rejected: 0 },
      });
    } finally {
      this.loading.set(false);
    }
  }
}

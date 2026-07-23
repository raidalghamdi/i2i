import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../shared/icon/icon.component';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { RoleKpiCardComponent } from '../../shared/role-kpi-card/role-kpi-card.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { DashboardApiService } from '../../core/dashboard-api.service';
import { AdminDashboard } from '../../core/dashboard.model';

@Component({
  selector: 'app-admin-dashboard',
  imports: [RouterLink, IconComponent, PageHeaderComponent, RoleKpiCardComponent, LoadingStateComponent],
  templateUrl: './admin-dashboard.component.html',
})
export class AdminDashboardComponent implements OnInit {
  private readonly api = inject(DashboardApiService);
  readonly data = signal<AdminDashboard | null>(null);
  readonly loading = signal(true);
  readonly usersLabel = $localize`:@@adminDashUsers:Total Users`;
  readonly activeLabel = $localize`:@@adminDashActive:Active Ideas`;
  readonly pendingLabel = $localize`:@@adminDashPending:Pending Evaluations`;
  readonly healthLabel = $localize`:@@adminDashHealth:System Health`;

  async ngOnInit(): Promise<void> {
    try {
      this.data.set(await this.api.getAdmin());
    } catch {
      // Degrade gracefully rather than blocking the admin hub — the "Warning"
      // health value already signals the fetch problem in the KPI tile.
      this.data.set({ totalUsers: 0, activeIdeas: 0, pendingEvaluations: 0, health: 'Warning' });
    } finally {
      this.loading.set(false);
    }
  }

  healthText(): string {
    return this.data()?.health === 'Warning' ? $localize`:@@adminDashWarning:Warning` : $localize`:@@adminDashHealthy:Healthy`;
  }
}

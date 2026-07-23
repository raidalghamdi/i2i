import { Component, OnInit, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { IdentityService } from '../../core/auth/identity.service';
import { DashboardComponent } from '../dashboard.component';
import { CommitteeDashboardComponent } from '../../committee/committee-dashboard/committee-dashboard.component';
import { SupervisorKpiDashboardComponent } from '../../supervisor/supervisor-kpi-dashboard/supervisor-kpi-dashboard.component';
import { AdminDashboardComponent } from '../../admin/admin-dashboard/admin-dashboard.component';

@Component({
  selector: 'app-dashboard-router',
  imports: [DashboardComponent, CommitteeDashboardComponent, SupervisorKpiDashboardComponent, AdminDashboardComponent],
  templateUrl: './dashboard-router.component.html',
})
export class DashboardRouterComponent implements OnInit {
  private readonly identity = inject(IdentityService);
  private readonly router = inject(Router);
  readonly role = computed(() => this.identity.identity()?.activeRole ?? null);

  ngOnInit(): void {
    if (this.role() === 'evaluator') void this.router.navigate(['/evaluator']);
  }
}

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AdminDashboard, CommitteeDashboard, SupervisorDashboard } from './dashboard.model';

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly http = inject(HttpClient);

  getAdmin(): Promise<AdminDashboard> {
    return firstValueFrom(this.http.get<AdminDashboard>('/api/dashboard/admin'));
  }

  getCommittee(): Promise<CommitteeDashboard> {
    return firstValueFrom(this.http.get<CommitteeDashboard>('/api/dashboard/committee'));
  }

  getSupervisor(): Promise<SupervisorDashboard> {
    return firstValueFrom(this.http.get<SupervisorDashboard>('/api/dashboard/supervisor'));
  }
}

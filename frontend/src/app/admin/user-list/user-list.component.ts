import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { AdminApiService } from '../admin-api.service';
import { AdminUser, PendingRoleGrant, RoleOption } from '../admin.model';

@Component({
  selector: 'app-user-list',
  imports: [RouterLink, FormsModule, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './user-list.component.html',
})
export class UserListComponent implements OnInit {
  private readonly adminApi = inject(AdminApiService);

  readonly users = signal<AdminUser[]>([]);
  readonly pendingGrants = signal<PendingRoleGrant[]>([]);
  roleOptions = signal<RoleOption[]>([]);
  grantSamAccountName = signal<string>('');
  grantRoleCode = signal<string>('');
  grantMessage = signal<string | null>(null);
  grantErrorMessage = signal<string | null>(null);
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
      this.users.set(await this.adminApi.listUsers());
      this.pendingGrants.set(await this.adminApi.listPendingGrants());
      this.roleOptions.set(await this.adminApi.listRoles());
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@userListLoadError:Couldn't load users. Please try again.`),
      );
    } finally {
      this.loading.set(false);
    }
  }

  async onCancelPendingGrant(id: string): Promise<void> {
    await this.adminApi.cancelPendingGrant(id);
    this.pendingGrants.set(await this.adminApi.listPendingGrants());
  }

  async onGrantBySamAccountName(): Promise<void> {
    const samAccountName = this.grantSamAccountName();
    const roleCode = this.grantRoleCode();
    if (!samAccountName || !roleCode) return;
    this.grantErrorMessage.set(null);
    this.grantMessage.set(null);
    try {
      const result = await this.adminApi.grantRole({ samAccountName, roleCode });
      this.grantMessage.set(result.status === 'granted' ? $localize`Role granted.` : $localize`Role will apply on their first login.`);
      this.users.set(await this.adminApi.listUsers());
      this.pendingGrants.set(await this.adminApi.listPendingGrants());
      this.grantSamAccountName.set('');
      this.grantRoleCode.set('');
    } catch (error) {
      this.grantErrorMessage.set(this.extractErrorMessage(error));
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

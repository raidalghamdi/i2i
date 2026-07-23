import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { AdminApiService } from '../admin-api.service';
import { AdminUser, RoleOption } from '../admin.model';

@Component({
  selector: 'app-user-detail',
  imports: [FormsModule, PageHeaderComponent, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './user-detail.component.html',
})
export class UserDetailComponent implements OnInit {
  private readonly adminApi = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly userId = this.route.snapshot.paramMap.get('id')!;

  readonly user = signal<AdminUser | null>(null);
  readonly roleOptions = signal<RoleOption[]>([]);
  readonly selectedRoleCode = signal<string>('');
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
      this.user.set(await this.adminApi.getUser(this.userId));
      this.roleOptions.set(await this.adminApi.listRoles());
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@userDetailLoadError:Couldn't load this user. Please try again.`),
      );
    } finally {
      this.loading.set(false);
    }
  }

  async onGrantRole(): Promise<void> {
    const current = this.user();
    const roleCode = this.selectedRoleCode();
    if (!current || !roleCode) return;
    this.errorMessage.set(null);
    try {
      await this.adminApi.grantRole({ samAccountName: current.samAccountName, roleCode });
      this.user.set(await this.adminApi.getUser(this.userId));
      this.selectedRoleCode.set('');
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onRevokeRole(roleId: string): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.adminApi.revokeRole(this.userId, roleId);
      this.user.set(await this.adminApi.getUser(this.userId));
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onToggleActive(): Promise<void> {
    const current = this.user();
    if (!current) return;
    this.errorMessage.set(null);
    try {
      await this.adminApi.setActive(this.userId, !current.isActive);
      this.user.set(await this.adminApi.getUser(this.userId));
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

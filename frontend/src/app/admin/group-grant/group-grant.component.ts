import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { AdminApiService } from '../admin-api.service';
import { GroupGrantResult, RoleOption } from '../admin.model';

@Component({
  selector: 'app-group-grant',
  imports: [FormsModule, PageHeaderComponent, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './group-grant.component.html',
})
export class GroupGrantComponent implements OnInit {
  private readonly adminApi = inject(AdminApiService);

  readonly roleOptions = signal<RoleOption[]>([]);
  readonly groupName = signal<string>('');
  readonly selectedRoleCode = signal<string>('');
  readonly result = signal<GroupGrantResult | null>(null);
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
      this.roleOptions.set(await this.adminApi.listRoles());
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@groupGrantLoadError:Couldn't load roles. Please try again.`),
      );
    } finally {
      this.loading.set(false);
    }
  }

  async onSubmit(): Promise<void> {
    const groupName = this.groupName();
    const roleCode = this.selectedRoleCode();
    if (!groupName || !roleCode) return;
    this.errorMessage.set(null);
    try {
      this.result.set(await this.adminApi.grantRoleToGroup({ groupName, roleCode }));
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

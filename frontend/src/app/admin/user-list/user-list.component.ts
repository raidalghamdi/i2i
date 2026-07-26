import { Component, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { DirectoryPickerComponent } from '../../shared/directory-picker/directory-picker.component';
import { DirectoryPerson } from '../../core/directory-api.service';
import { AdminApiService } from '../admin-api.service';
import { AdminUser, DirectoryImportResult, PendingRoleGrant, RoleOption } from '../admin.model';
import { StatusLabelPipe } from '../../shared/status-label/status-label.pipe';

@Component({
  selector: 'app-user-list',
  imports: [
    RouterLink,
    FormsModule,
    PageHeaderComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    DirectoryPickerComponent,
    StatusLabelPipe,
  ],
  templateUrl: './user-list.component.html',
})
export class UserListComponent implements OnInit {
  private readonly adminApi = inject(AdminApiService);

  @ViewChild(DirectoryPickerComponent) importPicker?: DirectoryPickerComponent;

  readonly users = signal<AdminUser[]>([]);
  readonly pendingGrants = signal<PendingRoleGrant[]>([]);
  roleOptions = signal<RoleOption[]>([]);
  grantSamAccountName = signal<string>('');
  grantRoleCode = signal<string>('');
  grantMessage = signal<string | null>(null);
  grantErrorMessage = signal<string | null>(null);
  readonly importSelected = signal<DirectoryPerson[]>([]);
  readonly importRoleCode = signal<string>('');
  readonly importResults = signal<DirectoryImportResult[] | null>(null);
  readonly importErrorMessage = signal<string | null>(null);
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

  onImportSelectionChange(selection: DirectoryPerson | DirectoryPerson[]): void {
    this.importSelected.set(Array.isArray(selection) ? selection : [selection]);
  }

  async onImportFromAd(): Promise<void> {
    const people = this.importSelected();
    const roleCode = this.importRoleCode();
    if (people.length === 0 || !roleCode) return;
    this.importErrorMessage.set(null);
    try {
      const results = await this.adminApi.importFromAd(
        people.map((p) => p.samAccountName),
        roleCode,
      );
      this.importResults.set(results);
      this.users.set(await this.adminApi.listUsers());
      this.pendingGrants.set(await this.adminApi.listPendingGrants());
      this.importSelected.set([]);
      this.importRoleCode.set('');
      this.importPicker?.clearSelection();
    } catch (error) {
      this.importResults.set(null);
      this.importErrorMessage.set(this.extractErrorMessage(error));
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

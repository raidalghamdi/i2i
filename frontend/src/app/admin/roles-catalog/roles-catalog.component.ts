import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IconComponent } from '../../shared/icon/icon.component';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { RolesCatalogApiService } from '../roles-catalog-api.service';
import { RoleCatalogPatch, RoleCatalogRow } from '../roles-catalog.model';

export interface EditableRoleRow {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
  isSystem: boolean;
  isActive: boolean;
  sortOrder: number;
}

@Component({
  selector: 'app-roles-catalog',
  imports: [FormsModule, PageHeaderComponent, IconComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './roles-catalog.component.html',
})
export class RolesCatalogComponent implements OnInit {
  private readonly api = inject(RolesCatalogApiService);

  readonly rows = signal<RoleCatalogRow[]>([]);
  readonly editableRows = signal<EditableRoleRow[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const roles = await this.api.list();
      this.rows.set(roles);
      this.editableRows.set(roles.map((role) => this.toEditableRow(role)));
    } catch (error) {
      this.loadError.set(this.extractErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  updateRow(id: string, patch: Partial<EditableRoleRow>): void {
    this.editableRows.update((rows) => rows.map((row) => (row.id === id ? { ...row, ...patch } : row)));
  }

  async onSave(row: EditableRoleRow): Promise<void> {
    this.errorMessage.set(null);
    const patch: RoleCatalogPatch = {
      nameAr: row.nameAr,
      nameEn: row.nameEn,
      descriptionAr: row.descriptionAr ? row.descriptionAr : null,
      descriptionEn: row.descriptionEn ? row.descriptionEn : null,
      isActive: row.isActive,
      sortOrder: Number(row.sortOrder),
    };

    try {
      await this.api.patch(row.id, patch);
      await this.reload();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  private toEditableRow(role: RoleCatalogRow): EditableRoleRow {
    return {
      id: role.id,
      code: role.code,
      nameAr: role.nameAr,
      nameEn: role.nameEn,
      descriptionAr: role.descriptionAr ?? '',
      descriptionEn: role.descriptionEn ?? '',
      isSystem: role.isSystem,
      isActive: role.isActive,
      sortOrder: role.sortOrder,
    };
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`Something went wrong. Please try again.`;
  }
}

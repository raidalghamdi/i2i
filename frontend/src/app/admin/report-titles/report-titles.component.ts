import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { ReportTitlesApiService } from './report-titles-api.service';
import { ReportTitle, ReportTitleInput, ReportTitlePatch } from './report-titles.model';

export interface EditableTitleRow {
  id: string;
  key: string;
  titleAr: string;
  titleEn: string;
  sortOrder: number;
}

@Component({
  selector: 'app-report-titles',
  imports: [FormsModule, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './report-titles.component.html',
})
export class ReportTitlesComponent implements OnInit {
  private readonly api = inject(ReportTitlesApiService);

  readonly rows = signal<ReportTitle[]>([]);
  readonly editableRows = signal<EditableTitleRow[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  readonly newKey = signal('');
  readonly newTitleAr = signal('');
  readonly newTitleEn = signal('');
  readonly newSortOrder = signal(0);

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const titles = await this.api.list();
      this.rows.set(titles);
      this.editableRows.set(titles.map((title) => this.toEditableRow(title)));
    } catch (error) {
      this.loadError.set(this.extractErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  updateRow(id: string, patch: Partial<EditableTitleRow>): void {
    this.editableRows.update((rows) => rows.map((row) => (row.id === id ? { ...row, ...patch } : row)));
  }

  async onSave(row: EditableTitleRow): Promise<void> {
    this.errorMessage.set(null);
    const patch: ReportTitlePatch = {
      titleAr: row.titleAr,
      titleEn: row.titleEn,
      sortOrder: Number(row.sortOrder),
    };

    try {
      await this.api.update(row.id, patch);
      await this.reload();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onDelete(id: string): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.api.remove(id);
      await this.reload();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onAdd(): Promise<void> {
    this.errorMessage.set(null);
    const input: ReportTitleInput = {
      key: this.newKey(),
      titleAr: this.newTitleAr(),
      titleEn: this.newTitleEn(),
      sortOrder: Number(this.newSortOrder()),
    };

    try {
      await this.api.create(input);
      this.newKey.set('');
      this.newTitleAr.set('');
      this.newTitleEn.set('');
      this.newSortOrder.set(0);
      await this.reload();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  private toEditableRow(title: ReportTitle): EditableTitleRow {
    return {
      id: title.id,
      key: title.key,
      titleAr: title.titleAr,
      titleEn: title.titleEn,
      sortOrder: title.sortOrder,
    };
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@reportTitlesError:Something went wrong. Please try again.`;
  }
}

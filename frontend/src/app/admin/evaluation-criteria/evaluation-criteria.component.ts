import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { EvaluationCriteriaApiService } from '../evaluation-criteria-api.service';
import { EvaluationCriterion, EvaluationCriterionInput } from '../evaluation-criteria.model';

// Change 20260726
export interface EditableEvaluationCriterionRow {
  localKey: string;
  id: string | null;
  code: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
  weight: number;
  active: boolean;
  sortOrder: number;
}

@Component({
  selector: 'app-evaluation-criteria',
  imports: [FormsModule, DecimalPipe, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './evaluation-criteria.component.html',
})
export class EvaluationCriteriaComponent implements OnInit {
  private readonly api = inject(EvaluationCriteriaApiService);
  private newRowSeq = 0;

  readonly rows = signal<EvaluationCriterion[]>([]);
  readonly editableRows = signal<EditableEvaluationCriterionRow[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  readonly activeWeightSum = computed(() =>
    this.editableRows()
      .filter((row) => row.active)
      .reduce((sum, row) => sum + (Number(row.weight) || 0), 0),
  );

  protected readonly Math = Math;

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const criteria = await this.api.list();
      this.rows.set(criteria);
      this.editableRows.set(criteria.map((criterion) => this.toEditableRow(criterion)));
    } catch (error) {
      this.loadError.set(this.extractErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  onAddRow(): void {
    this.newRowSeq += 1;
    this.editableRows.update((rows) => [
      ...rows,
      {
        localKey: `new-${this.newRowSeq}`,
        id: null,
        code: '',
        nameAr: '',
        nameEn: '',
        descriptionAr: '',
        descriptionEn: '',
        weight: 0,
        active: true,
        sortOrder: rows.length + 1,
      },
    ]);
  }

  updateRow(localKey: string, patch: Partial<EditableEvaluationCriterionRow>): void {
    this.editableRows.update((rows) =>
      rows.map((row) => (row.localKey === localKey ? { ...row, ...patch } : row)),
    );
  }

  async onSave(row: EditableEvaluationCriterionRow): Promise<void> {
    this.errorMessage.set(null);
    const input: EvaluationCriterionInput = {
      code: row.code,
      nameAr: row.nameAr,
      nameEn: row.nameEn,
      descriptionAr: row.descriptionAr ? row.descriptionAr : null,
      descriptionEn: row.descriptionEn ? row.descriptionEn : null,
      weight: Number(row.weight),
      active: row.active,
      sortOrder: Number(row.sortOrder),
    };

    try {
      if (row.id) {
        await this.api.update(row.id, input);
      } else {
        await this.api.create(input);
      }
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

  private toEditableRow(criterion: EvaluationCriterion): EditableEvaluationCriterionRow {
    return {
      localKey: criterion.id,
      id: criterion.id,
      code: criterion.code,
      nameAr: criterion.nameAr,
      nameEn: criterion.nameEn,
      descriptionAr: criterion.descriptionAr ?? '',
      descriptionEn: criterion.descriptionEn ?? '',
      weight: criterion.weight,
      active: criterion.active,
      sortOrder: criterion.sortOrder,
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

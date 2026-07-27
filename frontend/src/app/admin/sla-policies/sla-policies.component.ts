import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { SlaPoliciesApiService } from '../sla-policies-api.service';
import { SlaPolicy, SlaPolicyInput } from '../sla-policies.model';

// Change 20260726
export interface EditableSlaPolicyRow {
  localKey: string;
  id: string | null;
  entityType: string;
  fromState: string;
  toState: string;
  targetHours: number;
  warnAtPct: number;
}

@Component({
  selector: 'app-sla-policies',
  imports: [FormsModule, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './sla-policies.component.html',
})
export class SlaPoliciesComponent implements OnInit {
  private readonly api = inject(SlaPoliciesApiService);
  private newRowSeq = 0;

  readonly rows = signal<SlaPolicy[]>([]);
  readonly editableRows = signal<EditableSlaPolicyRow[]>([]);
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
      const policies = await this.api.list();
      this.rows.set(policies);
      this.editableRows.set(policies.map((policy) => this.toEditableRow(policy)));
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
        entityType: '',
        fromState: '',
        toState: '',
        targetHours: 24,
        warnAtPct: 80,
      },
    ]);
  }

  updateRow(localKey: string, patch: Partial<EditableSlaPolicyRow>): void {
    this.editableRows.update((rows) =>
      rows.map((row) => (row.localKey === localKey ? { ...row, ...patch } : row)),
    );
  }

  async onSave(row: EditableSlaPolicyRow): Promise<void> {
    this.errorMessage.set(null);
    const input: SlaPolicyInput = {
      entityType: row.entityType,
      fromState: row.fromState,
      toState: row.toState,
      targetHours: Number(row.targetHours),
      warnAtPct: Number(row.warnAtPct),
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

  private toEditableRow(policy: SlaPolicy): EditableSlaPolicyRow {
    return {
      localKey: policy.id,
      id: policy.id,
      entityType: policy.entityType,
      fromState: policy.fromState,
      toState: policy.toState,
      targetHours: policy.targetHours,
      warnAtPct: policy.warnAtPct,
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

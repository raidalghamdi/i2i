import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { PhaseScheduleApiService } from '../phase-schedule-api.service';
import { PhaseSchedule } from '../phase-schedule.model';

type PhaseStatus = 'active' | 'past' | 'future' | 'unscheduled';

@Component({
  selector: 'app-phase-schedule-editor',
  imports: [FormsModule, StatusBadgeComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './phase-schedule-editor.component.html',
})
export class PhaseScheduleEditorComponent implements OnInit {
  private readonly api = inject(PhaseScheduleApiService);

  readonly phases = signal<PhaseSchedule[]>([]);
  readonly saving = signal<Record<number, boolean>>({});
  readonly saved = signal<Record<number, boolean>>({});
  readonly errors = signal<Record<number, string>>({});
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
      this.phases.set(await this.api.list());
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@phaseScheduleEditorLoadError:Couldn't load phase schedules. Please try again.`),
      );
    } finally {
      this.loading.set(false);
    }
  }

  toLocalInput(iso: string | null): string {
    if (!iso) return '';
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return '';
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  updateStartsAt(idx: number, value: string): void {
    this.patchPhase(idx, { startsAt: this.fromLocalInput(value) });
  }

  updateEndsAt(idx: number, value: string): void {
    this.patchPhase(idx, { endsAt: this.fromLocalInput(value) });
  }

  status(phase: PhaseSchedule): PhaseStatus {
    const now = Date.now();
    const s = phase.startsAt ? new Date(phase.startsAt).getTime() : null;
    const e = phase.endsAt ? new Date(phase.endsAt).getTime() : null;
    if (s === null && e === null) return 'unscheduled';
    if (s !== null && now < s) return 'future';
    if (e !== null && now >= e) return 'past';
    return 'active';
  }

  async save(idx: number): Promise<void> {
    const phase = this.phases().find((p) => p.idx === idx);
    if (!phase) return;
    this.saving.update((map) => ({ ...map, [idx]: true }));
    this.errors.update((map) => ({ ...map, [idx]: '' }));
    try {
      const updated = await this.api.update(idx, { startsAt: phase.startsAt, endsAt: phase.endsAt });
      this.phases.update((list) => list.map((p) => (p.idx === idx ? updated : p)));
      this.saved.update((map) => ({ ...map, [idx]: true }));
    } catch (error) {
      this.errors.update((map) => ({ ...map, [idx]: this.extractErrorMessage(error) }));
    } finally {
      this.saving.update((map) => ({ ...map, [idx]: false }));
    }
  }

  private fromLocalInput(value: string): string | null {
    if (!value) return null;
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return null;
    return d.toISOString();
  }

  private patchPhase(idx: number, patch: Partial<PhaseSchedule>): void {
    this.phases.update((list) => list.map((p) => (p.idx === idx ? { ...p, ...patch } : p)));
    this.saved.update((map) => ({ ...map, [idx]: false }));
  }

  private extractErrorMessage(error: unknown, fallback = $localize`Something went wrong. Please try again.`): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return fallback;
  }
}

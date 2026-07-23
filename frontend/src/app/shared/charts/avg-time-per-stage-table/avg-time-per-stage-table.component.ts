import { Component, computed, input } from '@angular/core';

export interface AvgTimePerStageDatum {
  stage: number;
  avgDays: number;
}

interface RowView {
  stage: number;
  avgDaysLabel: string;
  widthPct: number;
}

/** Port of legacy `AvgTimePerStageTable` (executive-analytics.tsx): per-stage
 * average cycle-time table with a mini-bar, excluding the draft stage (0). */
@Component({
  selector: 'app-avg-time-per-stage-table',
  templateUrl: './avg-time-per-stage-table.component.html',
})
export class AvgTimePerStageTableComponent {
  readonly data = input.required<AvgTimePerStageDatum[]>();

  readonly rows = computed<RowView[]>(() => {
    const rows = this.data().filter((r) => r.stage > 0);
    const max = Math.max(...rows.map((r) => r.avgDays), 1);
    return rows.map((row) => ({
      stage: row.stage,
      avgDaysLabel: row.avgDays.toFixed(1),
      widthPct: Math.max((row.avgDays / max) * 100, 4),
    }));
  });
}

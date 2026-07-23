import { Component, computed, input } from '@angular/core';

export interface FunnelDatum {
  label: string;
  count: number;
}

interface FunnelRowView {
  label: string;
  count: number;
  widthPct: number;
}

/** Port of legacy `FunnelChart` (executive-analytics.tsx): CSS-div funnel bars
 * (Participation → Evaluated → Approved → Piloted → Scaled). */
@Component({
  selector: 'app-funnel-chart',
  templateUrl: './funnel-chart.component.html',
})
export class FunnelChartComponent {
  readonly data = input.required<FunnelDatum[]>();

  readonly rows = computed<FunnelRowView[]>(() => {
    const rows = this.data();
    const max = Math.max(...rows.map((r) => r.count), 1);
    return rows.map((row) => ({
      label: row.label,
      count: row.count,
      widthPct: Math.max((row.count / max) * 100, 6),
    }));
  });
}

import { Component, computed, input } from '@angular/core';

export interface CohortDatum {
  month: string;
  submitted: number;
  approved: number;
  rejected: number;
  implemented: number;
}

type SeriesKey = 'submitted' | 'approved' | 'rejected' | 'implemented';

interface SeriesDef {
  key: SeriesKey;
  color: string;
  label: string;
}

interface BarView {
  key: SeriesKey;
  x: number;
  y: number;
  width: number;
  height: number;
  color: string;
}

interface GroupView {
  month: string;
  label: string;
  labelX: number;
  bars: BarView[];
}

const HEIGHT = 200;
const PAD = { top: 10, bottom: 28, left: 28, right: 8 };

const SERIES: SeriesDef[] = [
  { key: 'submitted', color: '#20808D', label: $localize`:@@chartsCohortSubmitted:Submitted` },
  { key: 'approved', color: '#1B474D', label: $localize`:@@chartsCohortApproved:Approved` },
  { key: 'rejected', color: '#A84B2F', label: $localize`:@@chartsCohortRejected:Rejected` },
  { key: 'implemented', color: '#944454', label: $localize`:@@chartsCohortImplemented:Implemented` },
];

/** Port of legacy `CohortChart` (executive-analytics.tsx): pixel-faithful grouped
 * bar chart of the monthly idea cohort (submitted/approved/rejected/implemented). */
@Component({
  selector: 'app-cohort-chart',
  templateUrl: './cohort-chart.component.html',
})
export class CohortChartComponent {
  readonly data = input.required<CohortDatum[]>();

  readonly height = HEIGHT;
  readonly legend = SERIES;

  readonly width = computed(() => Math.max(this.data().length * 84, 320));

  readonly baselineY = HEIGHT - PAD.bottom;

  readonly groups = computed<GroupView[]>(() => {
    const rows = this.data();
    const chartWidth = this.width() - PAD.left - PAD.right;
    const chartHeight = HEIGHT - PAD.top - PAD.bottom;
    const groupWidth = rows.length ? chartWidth / rows.length : 0;
    const barWidth = Math.min(14, (groupWidth - 8) / SERIES.length);
    const barsTotalWidth = barWidth * SERIES.length;
    const max = Math.max(...rows.flatMap((r) => SERIES.map((s) => r[s.key])), 1);

    return rows.map((row, i) => {
      const groupX = PAD.left + i * groupWidth;
      const startX = groupX + (groupWidth - barsTotalWidth) / 2;
      const bars: BarView[] = SERIES.map((series, j) => {
        const value = row[series.key];
        const barHeight = (value / max) * chartHeight;
        return {
          key: series.key,
          x: startX + j * barWidth,
          y: PAD.top + (chartHeight - barHeight),
          width: barWidth,
          height: barHeight,
          color: series.color,
        };
      });
      return {
        month: row.month,
        label: row.month.slice(0, 7),
        labelX: groupX + groupWidth / 2,
        bars,
      };
    });
  });
}

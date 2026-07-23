import { Component, Inject, LOCALE_ID, computed, input } from '@angular/core';

export interface SubmissionsDatum {
  date: string;
  count: number;
}

interface PointView {
  x: number;
  y: number;
  count: number;
}

interface GridlineView {
  y: number;
  dashed: boolean;
}

interface XLabelView {
  x: number;
  text: string;
}

const WIDTH = 760;
const HEIGHT = 220;
const PAD = { top: 14, bottom: 32, left: 32, right: 12 };
const TEAL = '#20808D';
const FRACTIONS = [0, 0.25, 0.5, 0.75, 1];

/** Port of legacy `SubmissionsLineChart` (executive-analytics.tsx): pixel-faithful
 * line+area chart of daily idea submissions. */
@Component({
  selector: 'app-submissions-line-chart',
  templateUrl: './submissions-line-chart.component.html',
})
export class SubmissionsLineChartComponent {
  readonly data = input.required<SubmissionsDatum[]>();

  readonly width = WIDTH;
  readonly height = HEIGHT;
  readonly teal = TEAL;

  private readonly locale: string;

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.locale = locale;
  }

  private readonly chartWidth = WIDTH - PAD.left - PAD.right;
  private readonly chartHeight = HEIGHT - PAD.top - PAD.bottom;
  readonly baselineY = PAD.top + this.chartHeight;

  private readonly points = computed<PointView[]>(() => {
    const rows = this.data();
    const max = Math.max(...rows.map((r) => r.count), 1);
    const xStep = rows.length > 1 ? this.chartWidth / (rows.length - 1) : 0;
    return rows.map((row, i) => ({
      x: PAD.left + i * xStep,
      y: PAD.top + this.chartHeight - (row.count / max) * this.chartHeight,
      count: row.count,
    }));
  });

  readonly dots = computed(() => this.points().filter((p) => p.count > 0));

  readonly linePath = computed(() => {
    const pts = this.points();
    if (pts.length === 0) return '';
    return pts.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x},${p.y}`).join(' ');
  });

  readonly areaPath = computed(() => {
    const pts = this.points();
    if (pts.length === 0) return '';
    const line = this.linePath();
    const first = pts[0];
    const last = pts[pts.length - 1];
    return `${line} L ${last.x},${this.baselineY} L ${first.x},${this.baselineY} Z`;
  });

  readonly gridlines = computed<GridlineView[]>(() =>
    FRACTIONS.map((f) => ({
      y: PAD.top + this.chartHeight * (1 - f),
      dashed: f !== 0,
    })),
  );

  readonly xLabels = computed<XLabelView[]>(() => {
    const rows = this.data();
    const pts = this.points();
    if (rows.length === 0) return [];
    const labelStep = Math.max(1, Math.floor(rows.length / 6));
    const formatter = new Intl.DateTimeFormat(this.locale, { month: 'short', day: 'numeric' });
    const labels: XLabelView[] = [];
    rows.forEach((row, i) => {
      if (i % labelStep !== 0) return;
      const parsed = new Date(row.date);
      const text = Number.isNaN(parsed.getTime()) ? row.date : formatter.format(parsed);
      labels.push({ x: pts[i].x, text });
    });
    return labels;
  });

  readonly gridX1 = PAD.left;
  readonly gridX2 = WIDTH - PAD.right;
}

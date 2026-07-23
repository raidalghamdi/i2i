import { Component, computed, input } from '@angular/core';

export interface IdeasByStageDatum {
  stage: number;
  count: number;
}

interface BarView {
  stage: number;
  count: number;
  x: number;
  y: number;
  width: number;
  height: number;
  labelX: number;
  countLabelY: number;
  stageLabelY: number;
  nameLabelY: number;
  name: string;
}

const WIDTH_HEIGHT = 220;
const PAD = { top: 12, bottom: 44, left: 32, right: 8 };
const TEAL = '#20808D';
const MUTED = '#5C5F66';

/** Port of legacy `IdeasByStageChart` (executive-analytics.tsx): pixel-faithful
 * vertical bar chart of idea counts per pipeline stage (0..8). */
@Component({
  selector: 'app-ideas-by-stage-chart',
  templateUrl: './ideas-by-stage-chart.component.html',
})
export class IdeasByStageChartComponent {
  readonly data = input.required<IdeasByStageDatum[]>();
  readonly stageLabels = input<string[]>();

  readonly height = WIDTH_HEIGHT;
  readonly teal = TEAL;
  readonly muted = MUTED;

  readonly total = computed(() => this.data().reduce((sum, d) => sum + d.count, 0));

  readonly width = computed(() => Math.max(this.data().length * 56, 320));

  readonly baselineY = WIDTH_HEIGHT - PAD.bottom;

  readonly bars = computed<BarView[]>(() => {
    const rows = this.data();
    const chartWidth = this.width() - PAD.left - PAD.right;
    const chartHeight = WIDTH_HEIGHT - PAD.top - PAD.bottom;
    const groupWidth = rows.length ? chartWidth / rows.length : 0;
    const barWidth = Math.min(28, groupWidth - 10);
    const max = Math.max(...rows.map((r) => r.count), 1);
    const labels = this.stageLabels();

    return rows.map((row, i) => {
      const groupX = PAD.left + i * groupWidth;
      const barHeight = (row.count / max) * chartHeight;
      const x = groupX + (groupWidth - barWidth) / 2;
      const y = PAD.top + (chartHeight - barHeight);
      const name = (labels?.[row.stage] ?? String(row.stage)).slice(0, 10);
      return {
        stage: row.stage,
        count: row.count,
        x,
        y,
        width: barWidth,
        height: barHeight,
        labelX: groupX + groupWidth / 2,
        countLabelY: Math.max(y - 4, 10),
        stageLabelY: this.baselineY + 14,
        nameLabelY: this.baselineY + 26,
        name,
      };
    });
  });
}

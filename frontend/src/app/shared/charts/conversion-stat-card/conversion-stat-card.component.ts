import { DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';

/** Port of legacy `ConversionStatCard` (executive-analytics.tsx): submitted→pilot
 * conversion rate with a gradient meter and two supporting stat boxes. */
@Component({
  selector: 'app-conversion-stat-card',
  imports: [DecimalPipe],
  templateUrl: './conversion-stat-card.component.html',
})
export class ConversionStatCardComponent {
  readonly rate = input.required<number>();
  readonly submitted = input.required<number>();
  readonly pilot = input.required<number>();

  readonly clampedRate = computed(() => Math.min(100, Math.max(0, this.rate())));
}

import { Component, input } from '@angular/core';
import { IconComponent } from '../../shared/icon/icon.component';

@Component({
  selector: 'app-pioneer-badge',
  imports: [IconComponent],
  template: `
    @if (stage() >= 6) {
      <span
        class="inline-flex items-center gap-1 rounded-full border border-brand-gold/40 bg-brand-gold-light/70 px-2.5 py-0.5 text-[11px] font-semibold text-brand-teal"
      >
        <app-icon name="award" size="h-3 w-3" />
        <span i18n="@@pioneerBadge">Pioneer</span>
      </span>
    }
  `,
})
export class PioneerBadgeComponent {
  readonly stage = input.required<number>();
}

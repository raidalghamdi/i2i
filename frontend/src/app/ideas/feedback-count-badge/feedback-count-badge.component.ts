import { Component, input } from '@angular/core';
import { IconComponent } from '../../shared/icon/icon.component';

@Component({
  selector: 'app-feedback-count-badge',
  imports: [IconComponent],
  template: `
    @if (count() > 0) {
      <span
        class="inline-flex items-center gap-1 rounded-full border border-brand-teal/30 bg-brand-teal-light/70 px-2 py-0.5 text-[11px] font-semibold text-brand-teal"
      >
        <app-icon name="message-square" size="h-3 w-3" />{{ count() }}
        <span i18n="@@feedbackCountLabel">feedback</span>
      </span>
    }
  `,
})
export class FeedbackCountBadgeComponent {
  readonly count = input.required<number>();
}

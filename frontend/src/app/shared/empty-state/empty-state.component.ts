import { Component, input } from '@angular/core';
import { IconComponent, IconName } from '../icon/icon.component';

/** Reusable "nothing here" placeholder: icon, title, optional description,
 * and a projected CTA (e.g. "Submit an idea"). */
@Component({
  selector: 'app-empty-state',
  imports: [IconComponent],
  templateUrl: './empty-state.component.html',
})
export class EmptyStateComponent {
  readonly title = input.required<string>();
  readonly description = input<string>();
  readonly icon = input<IconName>('clipboard-list');
}

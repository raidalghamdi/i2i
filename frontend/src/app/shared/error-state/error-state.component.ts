import { Component, input, output } from '@angular/core';
import { IconComponent } from '../icon/icon.component';

/** Reusable inline error placeholder with a "Try again" retry action. */
@Component({
  selector: 'app-error-state',
  imports: [IconComponent],
  templateUrl: './error-state.component.html',
})
export class ErrorStateComponent {
  readonly message = input<string>();
  readonly retry = output<void>();

  protected readonly defaultMessage = $localize`:@@errorStateDefault:Something went wrong.`;
}

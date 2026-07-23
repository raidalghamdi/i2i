import { Component, input } from '@angular/core';

/** Reusable loading indicator: a centered CSS spinner, or `rows` pulsing
 * skeleton bars for list/table placeholders. */
@Component({
  selector: 'app-loading-state',
  templateUrl: './loading-state.component.html',
})
export class LoadingStateComponent {
  readonly label = input<string>();
  readonly variant = input<'spinner' | 'skeleton'>('spinner');
  readonly rows = input<number>(3);

  protected readonly defaultLabel = $localize`:@@loadingStateDefault:Loading…`;

  protected get skeletonRows(): number[] {
    return Array.from({ length: this.rows() }, (_, i) => i);
  }
}

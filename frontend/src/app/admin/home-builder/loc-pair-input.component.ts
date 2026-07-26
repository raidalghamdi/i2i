import { Component, input, output } from '@angular/core';
import { Loc } from './home-builder.model';

/** Reusable AR/EN input pair for editing a single `Loc` field. Used across all per-type
 * homepage-section editors so the 12 content schemas don't each hand-roll bilingual inputs. */
@Component({
  selector: 'app-loc-pair-input',
  templateUrl: './loc-pair-input.component.html',
})
export class LocPairInputComponent {
  readonly label = input<string>();
  readonly value = input.required<Loc>();
  readonly multiline = input(false);
  readonly valueChange = output<Loc>();

  protected readonly arLabel = $localize`:@@locPairInputArLabel:AR`;
  protected readonly enLabel = $localize`:@@locPairInputEnLabel:EN`;

  updateAr(ar: string): void {
    this.valueChange.emit({ ...this.value(), ar });
  }

  updateEn(en: string): void {
    this.valueChange.emit({ ...this.value(), en });
  }
}

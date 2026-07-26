import { Component, TemplateRef, input, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';

/** Generic repeatable-rows wrapper: renders `items` via a caller-supplied `rowTemplate`
 * (context: `$implicit` = item, `index` = row index), plus add/remove controls. Reused for
 * every array-shaped homepage-section field (Loc arrays and object arrays alike). */
@Component({
  selector: 'app-repeatable-list',
  imports: [NgTemplateOutlet],
  templateUrl: './repeatable-list.component.html',
})
export class RepeatableListComponent<T> {
  readonly label = input<string>();
  readonly items = input.required<T[]>();
  readonly rowTemplate = input.required<TemplateRef<{ $implicit: T; index: number }>>();
  readonly addLabel = input<string>();
  readonly removeLabel = input<string>();
  readonly add = output<void>();
  readonly remove = output<number>();

  protected readonly defaultAddLabel = $localize`:@@repeatableListAdd:Add row`;
  protected readonly defaultRemoveLabel = $localize`:@@repeatableListRemove:Remove`;

  templateContext(item: T, index: number): { $implicit: T; index: number } {
    return { $implicit: item, index };
  }
}

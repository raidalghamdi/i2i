import { Component, computed, input, output } from '@angular/core';

/** Idea section keys the submitter may be allowed to re-edit when returned; used by both the
 * screening decision ("return") and the evaluation review decision ("return"). */
export type EditableSectionKey =
  | 'title'
  | 'problem_statement'
  | 'proposed_solution'
  | 'expected_benefits'
  | 'activity_id'
  | 'strategic_theme_id'
  | 'challenge'
  | 'participation_type'
  | 'team'
  | 'attachments';

/**
 * Fixed checkbox list over the editable-section keys. Controlled component: the parent owns the
 * selection as a plain `string[]` and passes it back in via `selectedSections`; this component
 * only renders and emits `selectionChange` on toggle.
 */
@Component({
  selector: 'app-section-multiselect',
  templateUrl: './section-multiselect.component.html',
})
export class SectionMultiselectComponent {
  readonly selectedSections = input<string[]>([]);
  readonly selectionChange = output<string[]>();

  readonly selectedSet = computed(() => new Set(this.selectedSections()));

  isSelected(key: EditableSectionKey): boolean {
    return this.selectedSet().has(key);
  }

  toggle(key: EditableSectionKey): void {
    const next = new Set(this.selectedSet());
    if (next.has(key)) next.delete(key);
    else next.add(key);
    this.selectionChange.emit(Array.from(next));
  }
}

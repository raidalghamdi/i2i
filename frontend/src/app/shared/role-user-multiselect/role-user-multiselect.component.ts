import { Component, computed, input, output } from '@angular/core';
import { RoleUser } from '../../supervisor/supervisor.model';

/**
 * Checkbox/chip multiselect over a role-scoped user list (evaluators, judges, …), fed by
 * `SupervisorApiService.getUsersByRole(role)`. Controlled component: the parent owns the
 * selection as a plain `string[]` and passes it back in via `selectedIds`; this component only
 * renders and emits `selectionChange` on toggle.
 */
@Component({
  selector: 'app-role-user-multiselect',
  templateUrl: './role-user-multiselect.component.html',
})
export class RoleUserMultiselectComponent {
  readonly users = input.required<RoleUser[]>();
  readonly selectedIds = input<string[]>([]);
  readonly selectionChange = output<string[]>();

  readonly selectedSet = computed(() => new Set(this.selectedIds()));

  isSelected(id: string): boolean {
    return this.selectedSet().has(id);
  }

  toggle(id: string): void {
    const next = new Set(this.selectedSet());
    if (next.has(id)) next.delete(id);
    else next.add(id);
    this.selectionChange.emit(Array.from(next));
  }
}

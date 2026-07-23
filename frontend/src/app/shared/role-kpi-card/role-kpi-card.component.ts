import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IconComponent, IconName } from '../icon/icon.component';

/** Port of legacy `role-kpi-card.tsx`: a compact KPI tile used across role dashboards. */
@Component({
  selector: 'app-role-kpi-card',
  imports: [RouterLink, IconComponent],
  templateUrl: './role-kpi-card.component.html',
})
export class RoleKpiCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string | number>();
  readonly icon = input<IconName>();
  readonly hint = input<string>();
  readonly link = input<string[]>();
}

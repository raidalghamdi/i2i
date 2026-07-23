import { Component, computed, input } from '@angular/core';

const TONES: Record<string, string> = {
  slate: 'bg-slate-100 text-slate-700',
  gray: 'bg-gray-100 text-gray-600',
  blue: 'bg-blue-50 text-blue-700',
  amber: 'bg-amber-50 text-amber-800',
  orange: 'bg-orange-50 text-orange-700',
  purple: 'bg-purple-50 text-purple-700',
  green: 'bg-emerald-50 text-emerald-700',
  red: 'bg-red-50 text-red-700',
  teal: 'bg-brand-teal-light text-brand-teal',
};

/** Maps every status code used across this app (idea lifecycle, escalations,
 * committee decisions, report generation, screening decisions) to a tone. */
const STATUS_TONE: Record<string, string> = {
  // Idea lifecycle
  draft: 'slate',
  submitted: 'blue',
  evaluation: 'amber',
  pass_awaiting_attachments: 'amber',
  evaluation_failed: 'red',
  committee: 'purple',
  pending_final_ranking: 'orange',
  rejected: 'red',
  returned: 'orange',
  approved: 'green',
  not_selected: 'slate',
  withdrawn: 'slate',
  // Legacy-only lifecycle stages (kept for visual parity with seeded demo data;
  // not driven by this app's own workflow yet)
  screening: 'blue',
  assigned: 'amber',
  in_pilot: 'purple',
  in_measurement: 'teal',
  in_scaling: 'blue',
  in_implementation: 'blue',
  benefits_tracking: 'teal',
  closed: 'slate',
  // Escalations
  open: 'red',
  acknowledged: 'amber',
  resolved: 'green',
  cancelled: 'slate',
  // Committee decisions
  deferred: 'amber',
  // Report generation
  pending: 'amber',
  completed: 'green',
  failed: 'red',
  // Generic active/inactive
  active: 'green',
  inactive: 'slate',
  // Phase schedule
  past: 'slate',
  future: 'blue',
  unscheduled: 'amber',
  // Pilots
  running: 'blue',
  // Compliance controls
  met: 'green',
  in_progress: 'amber',
  not_started: 'slate',
  not_applicable: 'slate',
  // Email log
  sent: 'green',
};

@Component({
  selector: 'app-status-badge',
  templateUrl: './status-badge.component.html',
})
export class StatusBadgeComponent {
  readonly status = input.required<string | null | undefined>();

  readonly toneClass = computed(() => {
    const code = (this.status() ?? '').toLowerCase();
    const tone = STATUS_TONE[code] ?? 'slate';
    return TONES[tone];
  });
}

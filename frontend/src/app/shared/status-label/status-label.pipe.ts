import { Pipe, PipeTransform } from '@angular/core';

/**
 * Maps a backend status code (idea lifecycle, escalations, reports, compliance, …) to its Arabic
 * label. The app ships Arabic-only, so labels are Arabic literals — an unknown code falls back to
 * the raw code so nothing silently disappears. Use as `{{ item.status | statusLabel }}`.
 */
const STATUS_LABELS: Record<string, string> = {
  // Idea lifecycle
  draft: 'مسودة',
  submitted: 'مُقدَّمة',
  screening: 'قيد الفرز',
  evaluation: 'قيد التقييم',
  evaluation_review: 'مراجعة التقييم',
  submitter_review: 'مراجعة مُقدّم الفكرة',
  pass_awaiting_attachments: 'بانتظار المرفقات',
  evaluation_failed: 'لم تجتَز التقييم',
  committee: 'اللجنة',
  committee_pending: 'بانتظار اللجنة',
  pending_final_ranking: 'بانتظار الترتيب النهائي',
  rejected: 'مرفوضة',
  returned: 'مُعادة للتعديل',
  approved: 'معتمدة',
  not_selected: 'غير مختارة',
  withdrawn: 'مسحوبة',
  // Legacy / post-program lifecycle stages
  assigned: 'مُسندة',
  in_pilot: 'قيد التجربة',
  in_measurement: 'قيد القياس',
  in_scaling: 'قيد التوسّع',
  in_implementation: 'قيد التنفيذ',
  benefits_tracking: 'تتبّع الفوائد',
  closed: 'مغلقة',
  // Escalations
  open: 'مفتوحة',
  acknowledged: 'تم الاطّلاع',
  resolved: 'تم الحل',
  cancelled: 'ملغاة',
  // Committee decisions
  deferred: 'مؤجَّلة',
  // Report / job generation
  pending: 'قيد الانتظار',
  in_progress: 'قيد التنفيذ',
  completed: 'مكتملة',
  failed: 'فشلت',
  sent: 'تم الإرسال',
  // Generic active/inactive
  active: 'نشِطة',
  inactive: 'غير نشِطة',
  // Phase schedule
  past: 'منتهية',
  future: 'قادمة',
  unscheduled: 'غير مجدولة',
  // Pilots
  running: 'جارية',
  // Compliance controls
  met: 'مُستوفاة',
  not_started: 'لم تبدأ',
  not_applicable: 'لا ينطبق',
  // AD-import / role-grant outcomes (backend returns PascalCase; matched lower-cased)
  grantedimmediately: 'تم المنح فورًا',
  granted: 'تم المنح',
  alreadyassigned: 'مُسندة مسبقًا',
  skipped: 'تم التخطّي',
};

@Pipe({ name: 'statusLabel', standalone: true })
export class StatusLabelPipe implements PipeTransform {
  transform(code: string | null | undefined): string {
    const key = (code ?? '').toLowerCase();
    return STATUS_LABELS[key] ?? code ?? '';
  }
}

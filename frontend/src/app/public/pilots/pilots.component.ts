import { Component, Inject, LOCALE_ID } from '@angular/core';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';

interface Milestone {
  name: string;
  done: boolean;
}

interface Pilot {
  titleEn: string;
  titleAr: string;
  hypothesisEn: string;
  hypothesisAr: string;
  budget: number;
  status: string;
  results: string;
  milestones: readonly Milestone[];
}

const PILOTS: readonly Pilot[] = [
  {
    titleEn: 'Retailer complaints portal',
    titleAr: 'بوابة بلاغات تجار التجزئة',
    hypothesisEn: 'A guided portal increases SME complaints by 30%.',
    hypothesisAr: 'بوابة موجهة تزيد بلاغات المنشآت الصغيرة بنسبة 30%.',
    budget: 150000,
    status: 'running',
    results: 'Complaints up 24% at mid-review.',
    milestones: [
      { name: 'Launch', done: true },
      { name: 'Mid-review', done: true },
      { name: 'Final', done: false },
    ],
  },
  {
    titleEn: 'Unified price monitoring platform',
    titleAr: 'منصة موحدة لرصد الأسعار',
    hypothesisEn: 'A unified feed cuts detection lag in half.',
    hypothesisAr: 'التغذية الموحدة تقلل زمن الكشف للنصف.',
    budget: 220000,
    status: 'completed',
    results: 'Detection lag reduced 52%.',
    milestones: [
      { name: 'Data onboarding', done: true },
      { name: 'Model tuning', done: true },
    ],
  },
];

@Component({
  selector: 'app-pilots',
  imports: [PublicPageHeroComponent, StatusBadgeComponent],
  templateUrl: './pilots.component.html',
})
export class PilotsComponent {
  readonly pageTitle = $localize`:@@pilotsTitle:Pilot & experiment tracker`;
  readonly pageBody = $localize`:@@pilotsBody:Run real experiments and capture what worked.`;

  readonly hypothesisLabel = $localize`:@@pilotsHypothesisLabel:Hypothesis`;
  readonly budgetLabel = $localize`:@@pilotsBudgetLabel:Budget`;
  readonly milestonesLabel = $localize`:@@pilotsMilestonesLabel:Milestones`;
  readonly resultsLabel = $localize`:@@pilotsResultsLabel:Results`;
  readonly sarSuffix = $localize`:@@pilotsSarSuffix:SAR`;

  readonly pilots: readonly Pilot[] = PILOTS;

  private readonly isArabic: boolean;

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  title(pilot: Pilot): string {
    return this.isArabic ? pilot.titleAr : pilot.titleEn;
  }

  hypothesis(pilot: Pilot): string {
    return this.isArabic ? pilot.hypothesisAr : pilot.hypothesisEn;
  }

  statusLabel(status: string): string {
    switch (status) {
      case 'running':
        return $localize`:@@pilotsStatusRunning:Running`;
      case 'completed':
        return $localize`:@@pilotsStatusCompleted:Completed`;
      default:
        return status;
    }
  }
}

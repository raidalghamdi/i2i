import { Component } from '@angular/core';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

type MilestoneStatus = 'done' | 'active' | 'upcoming';

interface Milestone {
  phase: string;
  date: string;
  desc: string;
  status: MilestoneStatus;
}

const STATUS_LABEL: Record<MilestoneStatus, string> = {
  done: $localize`:@@roadmapStatusDone:Completed`,
  active: $localize`:@@roadmapStatusActive:In progress`,
  upcoming: $localize`:@@roadmapStatusUpcoming:Upcoming`,
};

const STATUS_TONE: Record<MilestoneStatus, string> = {
  done: 'bg-emerald-50 text-emerald-700',
  active: 'bg-amber-50 text-amber-800',
  upcoming: 'bg-slate-100 text-slate-700',
};

@Component({
  selector: 'app-roadmap',
  imports: [PublicPageHeroComponent],
  templateUrl: './roadmap.component.html',
})
export class RoadmapComponent {
  readonly pageTitle = $localize`:@@roadmapTitle:Program Roadmap`;
  readonly pageBody = $localize`:@@roadmapBody:Key phases and dates for the current program cycle.`;

  readonly milestones: readonly Milestone[] = [
    {
      phase: $localize`:@@roadmapPhase1:Submission Window Opens`,
      date: '2026-01-15',
      desc: $localize`:@@roadmapDesc1:Idea intake begins across all audiences.`,
      status: 'done',
    },
    {
      phase: $localize`:@@roadmapPhase2:Initial Screening`,
      date: '2026-03-01',
      desc: $localize`:@@roadmapDesc2:Completeness and fit checks on incoming ideas.`,
      status: 'active',
    },
    {
      phase: $localize`:@@roadmapPhase3:Technical Evaluation`,
      date: '2026-04-15',
      desc: $localize`:@@roadmapDesc3:Scoring by specialized evaluators.`,
      status: 'upcoming',
    },
    {
      phase: $localize`:@@roadmapPhase4:Committee Review`,
      date: '2026-05-15',
      desc: $localize`:@@roadmapDesc4:Final decisions on escalated ideas.`,
      status: 'upcoming',
    },
    {
      phase: $localize`:@@roadmapPhase5:Pilot Implementation`,
      date: '2026-07-01',
      desc: $localize`:@@roadmapDesc5:Approved ideas move into piloting.`,
      status: 'upcoming',
    },
    {
      phase: $localize`:@@roadmapPhase6:Impact Measurement`,
      date: '2026-10-01',
      desc: $localize`:@@roadmapDesc6:Outcomes measured and reported.`,
      status: 'upcoming',
    },
  ];

  statusLabel(status: MilestoneStatus): string {
    return STATUS_LABEL[status];
  }

  statusTone(status: MilestoneStatus): string {
    return STATUS_TONE[status];
  }
}

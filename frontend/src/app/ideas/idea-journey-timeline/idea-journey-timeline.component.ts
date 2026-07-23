import { Component, Inject, LOCALE_ID, computed, input } from '@angular/core';
import { JourneyStage, StageState } from '../idea.model';

@Component({
  selector: 'app-idea-journey-timeline',
  templateUrl: './idea-journey-timeline.component.html',
  styleUrl: './idea-journey-timeline.component.scss',
})
export class IdeaJourneyTimelineComponent {
  readonly stages = input.required<JourneyStage[]>();

  readonly sectionLabel = $localize`:@@ideaJourneySectionLabel:Idea journey`;

  private readonly isArabic: boolean;

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  private readonly fill = computed(() => {
    const list = this.stages();
    const segments = Math.max(list.length - 1, 1);
    const stoppedStage = list.find((s) => s.state === 'stopped');
    const lastCompleted = list.reduce((max, s) => (s.state === 'completed' ? Math.max(max, s.index) : max), -1);
    const fillToIdx = stoppedStage ? stoppedStage.index : lastCompleted;
    const fraction = Math.max(0, fillToIdx) / segments;
    return {
      width: `calc(${fraction} * 91%)`,
      color: stoppedStage ? 'var(--ij-stopped)' : 'var(--ij-completed)',
    };
  });

  get fillWidth(): string {
    return this.fill().width;
  }

  get fillColor(): string {
    return this.fill().color;
  }

  label(stage: JourneyStage): string {
    return this.isArabic ? stage.label.ar : stage.label.en;
  }

  stateLabel(state: StageState): string {
    switch (state) {
      case 'completed':
        return $localize`:@@ideaJourneyStateCompleted:Completed`;
      case 'current':
        return $localize`:@@ideaJourneyStateCurrent:Current`;
      case 'stopped':
        return $localize`:@@ideaJourneyStateStopped:Stopped`;
      case 'upcoming':
        return $localize`:@@ideaJourneyStateUpcoming:Upcoming`;
    }
  }
}

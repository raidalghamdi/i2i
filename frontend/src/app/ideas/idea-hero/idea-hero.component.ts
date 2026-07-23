import { Component, input } from '@angular/core';
import { IdeaJourney } from '../idea.model';
import { IdeaJourneyTimelineComponent } from '../idea-journey-timeline/idea-journey-timeline.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';

@Component({
  selector: 'app-idea-hero',
  imports: [IdeaJourneyTimelineComponent, StatusBadgeComponent],
  templateUrl: './idea-hero.component.html',
})
export class IdeaHeroComponent {
  readonly code = input.required<string>();
  readonly title = input.required<string>();
  readonly status = input.required<string>();
  readonly journey = input.required<IdeaJourney>();
  readonly trackName = input<string | null>(null);
  readonly activityName = input<string | null>(null);
  readonly challengeText = input<string | null>(null);
}

import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

interface EventCard {
  slug: string;
  title: string;
  desc: string;
}

@Component({
  selector: 'app-events',
  imports: [PublicPageHeroComponent, RouterLink],
  templateUrl: './events.component.html',
})
export class EventsComponent {
  readonly pageTitle = $localize`:@@eventsTitle:Events`;
  readonly pageBody = $localize`:@@eventsBody:Competitions, hackathons, and workshops throughout the program cycle.`;

  readonly viewDetailsLabel = $localize`:@@eventsViewDetailsLabel:View details`;

  readonly cards: readonly EventCard[] = [
    {
      slug: 'main',
      title: $localize`:@@eventsMainTitle:Main Competition`,
      desc: $localize`:@@eventsMainDesc:The flagship annual competition where top ideas are showcased and awarded. Winning ideas are fast-tracked to piloting.`,
    },
    {
      slug: 'hackathon',
      title: $localize`:@@eventsHackathonTitle:Innovation Hackathon`,
      desc: $localize`:@@eventsHackathonDesc:An intensive multi-day event where cross-functional teams prototype solutions to competition-related challenges.`,
    },
    {
      slug: 'workshops',
      title: $localize`:@@eventsWorkshopsTitle:Workshops`,
      desc: $localize`:@@eventsWorkshopsDesc:Hands-on sessions to strengthen ideas and build innovation skills.`,
    },
  ];
}

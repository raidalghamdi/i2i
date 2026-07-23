import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

interface EventSection {
  title: string;
  desc: string;
}

interface WorkshopItem {
  name: string;
  date: string;
}

const SECTIONS: Record<string, EventSection> = {
  main: {
    title: $localize`:@@eventSectionMainTitle:Main Competition`,
    desc: $localize`:@@eventSectionMainDesc:The flagship annual competition where top ideas are showcased and awarded. Winning ideas are fast-tracked to piloting.`,
  },
  hackathon: {
    title: $localize`:@@eventSectionHackathonTitle:Innovation Hackathon`,
    desc: $localize`:@@eventSectionHackathonDesc:An intensive multi-day event where cross-functional teams prototype solutions to competition-related challenges.`,
  },
  workshops: {
    title: $localize`:@@eventSectionWorkshopsTitle:Workshops`,
    desc: $localize`:@@eventSectionWorkshopsDesc:Hands-on sessions to strengthen ideas and build innovation skills.`,
  },
};

@Component({
  selector: 'app-event-section',
  imports: [RouterLink],
  templateUrl: './event-section.component.html',
})
export class EventSectionComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);

  readonly section = signal<EventSection | null>(null);
  readonly sectionKey = signal<string | null>(null);

  readonly upcomingSessionsLabel = $localize`:@@eventSectionUpcomingSessions:Upcoming sessions`;
  readonly notFoundMessage = $localize`:@@eventSectionNotFound:Event not found.`;
  readonly backToEventsLabel = $localize`:@@eventSectionBackToEvents:Back to Events`;

  readonly workshopItems: readonly WorkshopItem[] = [
    {
      name: $localize`:@@eventSectionWorkshopItem1Name:Idea Framing & Problem Definition`,
      date: '2026-02-10',
    },
    {
      name: $localize`:@@eventSectionWorkshopItem2Name:Building a Strong Evaluation Case`,
      date: '2026-03-05',
    },
    {
      name: $localize`:@@eventSectionWorkshopItem3Name:From Pilot to Scale`,
      date: '2026-04-20',
    },
  ];

  ngOnInit(): void {
    const key = this.route.snapshot.paramMap.get('section') ?? '';
    this.sectionKey.set(key);
    this.section.set(SECTIONS[key] ?? null);
  }
}

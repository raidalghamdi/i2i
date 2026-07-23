import { Component, Inject, LOCALE_ID, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IdentityService } from '../core/auth/identity.service';
import { PlatformStats, PlatformStatsService } from '../core/platform-stats.service';
import { IconComponent, IconName } from '../shared/icon/icon.component';
import { PublicTracksApiService } from '../core/public-tracks-api.service';
import { PublicTrack } from '../core/public-data.model';
import { HeroRotatorComponent } from './hero-rotator/hero-rotator.component';
import { HeroNetworkComponent } from './hero-network/hero-network.component';
import { TimelineModernComponent, TimelineStage } from './timeline-modern/timeline-modern.component';
import { SiteFooterComponent } from './site-footer/site-footer.component';
import { StickyCtaComponent } from './sticky-cta/sticky-cta.component';
import { BackToTopComponent } from './back-to-top/back-to-top.component';

interface CriterionItem {
  label: string;
  description: string;
  weight: number;
  color: string;
  icon: IconName;
}

interface PrizeItem {
  tier: string;
  value: string;
}

interface FaqItem {
  q: string;
  a: string;
}

@Component({
  selector: 'app-landing',
  imports: [RouterLink, IconComponent, HeroRotatorComponent, HeroNetworkComponent, TimelineModernComponent, SiteFooterComponent, StickyCtaComponent, BackToTopComponent],
  templateUrl: './landing.component.html',
})
export class LandingComponent implements OnInit {
  private readonly identityService = inject(IdentityService);
  private readonly platformStatsService = inject(PlatformStatsService);
  private readonly publicTracksApiService = inject(PublicTracksApiService);
  private readonly isArabic: boolean;
  readonly identity = this.identityService.identity;

  readonly stats = signal<PlatformStats | null>(null);
  readonly tracks = signal<PublicTrack[]>([]);

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  trackName(track: PublicTrack): string {
    return this.isArabic ? track.nameAr : track.nameEn;
  }

  trackDescription(track: PublicTrack): string {
    return this.isArabic ? track.descriptionAr : track.descriptionEn;
  }

  async ngOnInit(): Promise<void> {
    try {
      this.tracks.set(await this.publicTracksApiService.list());
    } catch {
      // The tracks section simply renders empty if the public tracks endpoint is unreachable.
    }

    const id = this.identity();
    if (!id || id.roles.length === 0) return;
    try {
      this.stats.set(await this.platformStatsService.get());
    } catch {
      // Platform-wide stats are a nice-to-have on the homepage; a fetch
      // failure here should not block the rest of the landing page.
    }
  }

  readonly heroWords = [
    $localize`Innovate`,
    $localize`Compete`,
    $localize`Impact`,
  ];

  readonly objectives = [
    $localize`Foster a culture of institutional innovation`,
    $localize`Accelerate turning ideas into actionable initiatives`,
    $localize`Strengthen collaboration across teams and entities`,
    $localize`Build a reusable knowledge base of solutions`,
    $localize`Measure the real-world impact of implemented ideas`,
  ];

  readonly rules = [
    $localize`Register individually or as a team of up to 5 members`,
    $localize`Every idea must fall under one of the approved tracks`,
    $localize`Each team member signs the IP terms independently`,
  ];

  readonly timelineStages: TimelineStage[] = [
    { id: 'registration-open', title: $localize`Registration opens`, date: $localize`1 August 2026`, description: $localize`Register individually or as a team, and choose your track.`, tone: 'cyan' },
    { id: 'registration-close', title: $localize`Registration closes`, date: $localize`15 September 2026`, description: $localize`Final deadline to receive participation requests.`, tone: 'cyan' },
    { id: 'teams-announced', title: $localize`Accepted teams announced`, date: $localize`20 September 2026`, description: $localize`Applications are screened and teams selected.`, tone: 'cyan' },
    { id: 'workshops', title: $localize`Qualification workshops`, date: $localize`25 Sep — 8 Oct`, description: $localize`Virtual workshops in design thinking.`, tone: 'cyan' },
    { id: 'hackathon', title: $localize`Hackathon days · 48 hours`, date: $localize`12 — 13 October`, description: $localize`The on-site innovation marathon.`, tone: 'gold' },
    { id: 'judging', title: $localize`Judging`, date: $localize`14 October — morning`, description: $localize`Solutions are presented to the judging panel.`, tone: 'gold' },
    { id: 'winners', title: $localize`Winners announced`, date: $localize`14 October — evening`, description: $localize`The closing and awards ceremony.`, tone: 'gold' },
  ];

  readonly criteria: CriterionItem[] = [
    { label: $localize`Innovation`, description: $localize`How novel and differentiated the idea is versus what already exists.`, weight: 25, color: '#01696F', icon: 'sparkles' },
    { label: $localize`Impact`, description: $localize`Expected value to competition, consumers, and the wider economy.`, weight: 25, color: '#20808D', icon: 'rocket' },
    { label: $localize`Feasibility`, description: $localize`Clarity of the plan, realism of the resources and the timeline.`, weight: 20, color: '#D19900', icon: 'wrench' },
    { label: $localize`Scalability`, description: $localize`Ability to expand geographically or across sectors without rebuilding.`, weight: 20, color: '#A84B2F', icon: 'expand' },
    { label: $localize`Presentation quality`, description: $localize`Sharpness of the summary, strength of the evidence, and pitch quality.`, weight: 10, color: '#7A7974', icon: 'presentation' },
  ];

  readonly prizes: PrizeItem[] = [
    { tier: $localize`First Place`, value: $localize`SAR 100,000 + implementation support` },
    { tier: $localize`Second Place`, value: $localize`SAR 60,000` },
    { tier: $localize`Third Place`, value: $localize`SAR 30,000` },
  ];

  readonly previousGallery = [
    $localize`Opening ceremony`,
    $localize`Workshops`,
    $localize`Pitches`,
    $localize`Judging panel`,
    $localize`Winners`,
    $localize`Group photo`,
  ];

  readonly partners = [
    $localize`Ministry of Commerce`,
    $localize`Monsha'at (SME Authority)`,
    $localize`SDAIA`,
    $localize`King Abdulaziz City for Science and Technology`,
    $localize`Saudi Data & AI Authority`,
    $localize`Local Universities`,
    $localize`Private Sector Chambers`,
    $localize`Innovation Hubs`,
  ];

  readonly faqItems: FaqItem[] = [
    { q: $localize`Who can submit an idea?`, a: $localize`Employees of the Authority, entrepreneurs, government entities, and innovation specialists are all welcome to submit.` },
    { q: $localize`How long does evaluation take?`, a: $localize`Initial check: a few working days. Full evaluation: two to three weeks.` },
    { q: $localize`Is my idea kept confidential?`, a: $localize`Yes. Ideas are handled according to the confidentiality level and reviewed only by authorized evaluators and committee members.` },
    { q: $localize`Can I submit more than one idea?`, a: $localize`Yes, there is no limit on the number of ideas you may submit.` },
    { q: $localize`What happens if my idea needs changes?`, a: $localize`You'll get feedback on what needs work and can update your idea and resubmit.` },
    { q: $localize`Do I retain ownership of my idea?`, a: $localize`Ownership terms are described in the IP terms you acknowledge when submitting.` },
    { q: $localize`Will I be notified of progress?`, a: $localize`Yes — you'll get a notification every time something happens with your idea.` },
    { q: $localize`What are the evaluation criteria?`, a: $localize`Ideas are scored on strategic alignment, innovation, feasibility, impact, and effort. See the Evaluation Criteria page.` },
  ];
}

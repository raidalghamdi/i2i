import { Component } from '@angular/core';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

interface AudienceCard {
  title: string;
  body: string;
}

@Component({
  selector: 'app-target-audience',
  imports: [PublicPageHeroComponent],
  templateUrl: './target-audience.component.html',
})
export class TargetAudienceComponent {
  readonly pageTitle = $localize`:@@targetAudienceTitle:Who Can Participate`;
  readonly pageBody = $localize`:@@targetAudienceBody:The program is open to anyone with a good idea.`;

  readonly cards: readonly AudienceCard[] = [
    {
      title: $localize`:@@targetAudienceCard1Title:Entrepreneurs & Startups`,
      body: $localize`:@@targetAudienceCard1Body:Founders and emerging companies with market-ready solutions relevant to competition, market monitoring, and consumer protection.`,
    },
    {
      title: $localize`:@@targetAudienceCard2Title:Government Entities`,
      body: $localize`:@@targetAudienceCard2Body:Public-sector teams seeking to co-develop regulatory and analytical tools, and to pilot cross-agency initiatives.`,
    },
    {
      title: $localize`:@@targetAudienceCard3Title:Innovation Specialists`,
      body: $localize`:@@targetAudienceCard3Body:Researchers, data scientists, and domain experts who can strengthen ideas with evidence, methods, and technical depth.`,
    },
  ];
}

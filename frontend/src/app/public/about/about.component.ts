import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

interface ParticipateCard {
  title: string;
  body: string;
}

@Component({
  selector: 'app-about',
  imports: [PublicPageHeroComponent, RouterLink],
  templateUrl: './about.component.html',
})
export class AboutComponent {
  readonly pageTitle = $localize`:@@aboutTitle:About the Program`;
  readonly pageBody = $localize`:@@aboutBody:Innovation to Impact — how the General Authority for Competition turns ideas into real results.`;

  readonly visionTitle = $localize`:@@aboutVisionTitle:Our Vision`;
  readonly visionBody = $localize`:@@aboutVisionBody:To build a culture of innovation across the General Authority for Competition — where every employee can shape a fairer, more competitive Saudi market aligned with Vision 2030.`;

  readonly missionTitle = $localize`:@@aboutMissionTitle:Our Mission`;
  readonly missionBody = $localize`:@@aboutMissionBody:To make sure good ideas from anywhere in GAC actually get built — with a fair process and visible results.`;

  readonly pipelineTitle = $localize`:@@aboutPipelineTitle:The 9-Stage Pipeline`;
  readonly pipelineBody = $localize`:@@aboutPipelineBody:Every idea follows the same path — submitted, reviewed by experts, decided by the committee, piloted, then rolled out. At each step, someone is responsible and the criteria are published, so you always know where your idea stands.`;
  readonly pipelineLinkText = $localize`:@@aboutPipelineLink:Explore all 9 stages`;

  readonly participateTitle = $localize`:@@aboutParticipateTitle:Who Can Participate`;
  readonly participateSubtitle = $localize`:@@aboutParticipateSubtitle:The platform is open to every part of the Authority.`;

  readonly participateCards: readonly ParticipateCard[] = [
    {
      title: $localize`:@@aboutParticipateCard1Title:GAC employees at all levels`,
      body: $localize`:@@aboutParticipateCard1Body:From frontline staff to leadership — anyone with an idea that can improve how the Authority delivers on its mandate.`,
    },
    {
      title: $localize`:@@aboutParticipateCard2Title:All departments and functions`,
      body: $localize`:@@aboutParticipateCard2Body:Legal, economic, market monitoring, technology, communications, HR, and every other function across the Authority.`,
    },
    {
      title: $localize`:@@aboutParticipateCard3Title:Individual contributors and teams`,
      body: $localize`:@@aboutParticipateCard3Body:Submit on your own or bring an idea forward as a team — the pipeline supports both individual and collaborative submissions.`,
    },
  ];

  readonly partnersTitle = $localize`:@@aboutPartnersTitle:Our Partners`;
  readonly partnersBody = $localize`:@@aboutPartnersBody:We collaborate with entities across government, academia, and the private sector.`;

  readonly contactTitle = $localize`:@@aboutContactTitle:Have a question?`;
  readonly contactBody = $localize`:@@aboutContactBody:Got a question about the program or want to talk through an idea before submitting? Drop us a line.`;
  readonly contactEmail = 'innovation@gac.gov.sa';
  readonly contactCtaText = $localize`:@@aboutContactCta:Contact the Innovation Office`;
}

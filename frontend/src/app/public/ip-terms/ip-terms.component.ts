import { Component } from '@angular/core';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

interface IpTermsSection {
  heading: string;
  body: string;
}

@Component({
  selector: 'app-ip-terms',
  imports: [PublicPageHeroComponent],
  templateUrl: './ip-terms.component.html',
})
export class IpTermsComponent {
  readonly pageTitle = $localize`:@@ipTermsTitle:Ownership & IP Terms`;
  readonly pageBody = $localize`:@@ipTermsBody:Terms and conditions governing ownership of ideas submitted to the Competition Program of the General Authority for Competition.`;
  readonly lastUpdated = $localize`:@@ipTermsLastUpdated:Last updated: 29 July 2025`;

  readonly sections: readonly IpTermsSection[] = [
    {
      heading: $localize`:@@ipTermsSection1Heading:1. Definitions`,
      body: $localize`:@@ipTermsSection1Body:"Idea" means any proposal, suggestion, innovation or solution submitted by a participant through the GAC Innovation Platform, whether written, visual, or accompanied by supporting documents. "Intellectual Property" includes any patent rights, copyrights, trademarks, trade secrets, industrial designs, and any related rights arising from the idea.`,
    },
    {
      heading: $localize`:@@ipTermsSection2Heading:2. Ownership of the Idea`,
      body: $localize`:@@ipTermsSection2Body:By submitting your idea, you agree that all intellectual property rights transfer to the General Authority for Competition.`,
    },
    {
      heading: $localize`:@@ipTermsSection3Heading:3. Originality Warranty`,
      body: $localize`:@@ipTermsSection3Body:The participant warrants that the submitted idea is their original work and that it does not infringe upon any third-party intellectual property rights. The participant bears full responsibility for any legal claims arising from allegations of infringement.`,
    },
    {
      heading: $localize`:@@ipTermsSection4Heading:4. Confidentiality`,
      body: $localize`:@@ipTermsSection4Body:GAC is committed to maintaining the confidentiality of submitted ideas according to the level of confidentiality designated by the participant (public, internal, or confidential). Confidential ideas are accessed only by the authorized evaluation team, solely for screening and evaluation purposes.`,
    },
    {
      heading: $localize`:@@ipTermsSection5Heading:5. Attribution`,
      body: $localize`:@@ipTermsSection5Body:GAC acknowledges the participant as the original author of the idea and may credit them in implementation documents and related publications, at its discretion.`,
    },
    {
      heading: $localize`:@@ipTermsSection6Heading:6. Incentives and Rewards`,
      body: $localize`:@@ipTermsSection6Body:If your idea is implemented, GAC may recognise you with a reward — financial or otherwise. Submitting doesn't automatically entitle you to one; any rewards follow GAC's incentive policy.`,
    },
    {
      heading: $localize`:@@ipTermsSection7Heading:7. Ownership Exceptions`,
      body: $localize`:@@ipTermsSection7Body:The ownership transfer terms do not apply to ideas submitted by external rights holders under pre-existing partnership or licensing agreements, or ideas submitted by other government entities subject to their own regulations. In such cases, ownership is determined per written agreement.`,
    },
    {
      heading: $localize`:@@ipTermsSection8Heading:8. Agreement`,
      body: $localize`:@@ipTermsSection8Body:By submitting your idea through the platform and agreeing to these terms, you acknowledge that you have read, understood, and agreed to all the terms above, and that you waive any right to claim ownership of the idea after submission as specified.`,
    },
    {
      heading: $localize`:@@ipTermsSection9Heading:9. Governing Law`,
      body: $localize`:@@ipTermsSection9Body:These terms are governed by the laws of the Kingdom of Saudi Arabia, particularly the Copyright Law, the Patents and Industrial Designs Law, the Trademarks Law, and the Competition Law. The competent courts in Riyadh have jurisdiction over any dispute arising from the application of these terms.`,
    },
  ];

  readonly agreementNote = $localize`:@@ipTermsAgreementNote:By clicking "I agree" on the idea submission page, you acknowledge that you have read and agreed to all the terms above.`;
}

import { Component } from '@angular/core';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

interface FaqItem {
  q: string;
  a: string;
}

@Component({
  selector: 'app-faq',
  imports: [PublicPageHeroComponent],
  templateUrl: './faq.component.html',
})
export class FaqComponent {
  readonly pageTitle = $localize`:@@faqTitle:Frequently Asked Questions`;
  readonly pageBody = $localize`:@@faqBody:Answers to common questions about the program.`;

  readonly items: readonly FaqItem[] = [
    {
      q: $localize`:@@faqQ1:Who can submit an idea?`,
      a: $localize`:@@faqA1:Employees of the Authority, entrepreneurs, government entities, and innovation specialists are all welcome to submit.`,
    },
    {
      q: $localize`:@@faqQ2:How long does evaluation take?`,
      a: $localize`:@@faqA2:Initial check: a few working days. Full evaluation: two to three weeks.`,
    },
    {
      q: $localize`:@@faqQ3:Is my idea kept confidential?`,
      a: $localize`:@@faqA3:Yes. Ideas are handled according to the confidentiality level and reviewed only by authorized evaluators and committee members.`,
    },
    {
      q: $localize`:@@faqQ4:Can I submit more than one idea?`,
      a: $localize`:@@faqA4:Yes, there is no limit on the number of ideas you may submit.`,
    },
    {
      q: $localize`:@@faqQ5:What happens if my idea needs changes?`,
      a: $localize`:@@faqA5:You'll get feedback on what needs work and can update your idea and resubmit.`,
    },
    {
      q: $localize`:@@faqQ6:Do I retain ownership of my idea?`,
      a: $localize`:@@faqA6:Ownership terms are described in the IP terms you acknowledge when submitting.`,
    },
    {
      q: $localize`:@@faqQ7:Will I be notified of progress?`,
      a: $localize`:@@faqA7:Yes — you'll get a notification every time something happens with your idea.`,
    },
    {
      q: $localize`:@@faqQ8:What are the evaluation criteria?`,
      a: $localize`:@@faqA8:Ideas are scored on strategic alignment, innovation, feasibility, impact, and effort. See the Evaluation Criteria page.`,
    },
    {
      q: $localize`:@@faqQ9:What support is available?`,
      a: $localize`:@@faqA9:You can reach the innovation team via the Support page for guidance at any stage.`,
    },
    {
      q: $localize`:@@faqQ10:Is there a submission deadline?`,
      a: $localize`:@@faqA10:Submission windows are announced on the Roadmap page and reflected in the countdown on the home page.`,
    },
  ];
}

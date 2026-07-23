import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

interface Stage {
  index: number;
  name: string;
  whatHappens: string;
  whatYouNeed: string;
  owner: string;
}

@Component({
  selector: 'app-stages',
  imports: [PublicPageHeroComponent, RouterLink],
  templateUrl: './stages.component.html',
})
export class StagesComponent {
  readonly pageTitle = $localize`:@@stagesTitle:The Nine Stages of Innovation`;
  readonly pageBody = $localize`:@@stagesBody:Here's how an idea moves from your head to something that actually changes how GAC operates.`;

  readonly stageLabel = $localize`:@@stagesStageLabel:Stage`;
  readonly whatHappensLabel = $localize`:@@stagesWhatHappensLabel:What happens here?`;
  readonly whatYouNeedLabel = $localize`:@@stagesWhatYouNeedLabel:What you need to do?`;
  readonly ownerLabel = $localize`:@@stagesOwnerLabel:Owner`;
  readonly viewIdeasLabel = $localize`:@@stagesViewIdeasLabel:View Ideas in This Stage`;

  readonly stage0Title = $localize`:@@stagesStage0Title:Strategic leadership`;
  readonly stage0Owner = $localize`:@@stagesOwner0:Strategic leadership`;

  readonly stages: readonly Stage[] = [
    {
      index: 1,
      name: $localize`:@@stagesName1:Idea Submission`,
      whatHappens: $localize`:@@stagesWhatHappens1:You submit your idea. It gets a reference number and enters the queue.`,
      whatYouNeed: $localize`:@@stagesWhatYouNeed1:Tell us your idea in your own words. Attach anything that helps explain it.`,
      owner: $localize`:@@stagesOwner1:Innovation team`,
    },
    {
      index: 2,
      name: $localize`:@@stagesName2:Initial Screening`,
      whatHappens: $localize`:@@stagesWhatHappens2:The team checks your submission meets the basics before sending it to specialist evaluators.`,
      whatYouNeed: $localize`:@@stagesWhatYouNeed2:Participate in events and campaigns linked to priorities.`,
      owner: $localize`:@@stagesOwner2:Events team`,
    },
    {
      index: 3,
      name: $localize`:@@stagesName3:Technical Evaluation`,
      whatHappens: $localize`:@@stagesWhatHappens3:Relevant experts read your idea and score it independently across five dimensions.`,
      whatYouNeed: $localize`:@@stagesWhatYouNeed3:Watch where your idea is in the process. The team may reach out if they need more from you.`,
      owner: $localize`:@@stagesOwner3:Screening team`,
    },
    {
      index: 4,
      name: $localize`:@@stagesName4:Committee Review`,
      whatHappens: $localize`:@@stagesWhatHappens4:The committee reviews the strongest ideas and makes the final call on each one.`,
      whatYouNeed: $localize`:@@stagesWhatYouNeed4:You may be asked for additional information or a short pitch.`,
      owner: $localize`:@@stagesOwner4:Evaluators & committee`,
    },
    {
      index: 5,
      name: $localize`:@@stagesName5:Approval`,
      whatHappens: $localize`:@@stagesWhatHappens5:Once approved, the idea gets an owner, a budget, and a delivery date.`,
      whatYouNeed: $localize`:@@stagesWhatYouNeed5:Connect with the assigned owner to implement your idea.`,
      owner: $localize`:@@stagesOwner5:Innovation manager`,
    },
    {
      index: 6,
      name: $localize`:@@stagesName6:Pilot Implementation`,
      whatHappens: $localize`:@@stagesWhatHappens6:The idea runs as a real experiment — tested in a real context before full rollout.`,
      whatYouNeed: $localize`:@@stagesWhatYouNeed6:Participate in the pilot and give feedback on results.`,
      owner: $localize`:@@stagesOwner6:Implementation team`,
    },
    {
      index: 7,
      name: $localize`:@@stagesName7:Measurement & Impact`,
      whatHappens: $localize`:@@stagesWhatHappens7:What actually changed? This stage captures the real-world difference the idea made.`,
      whatYouNeed: $localize`:@@stagesWhatYouNeed7:Collaborate with the adoption team for a wider rollout.`,
      owner: $localize`:@@stagesOwner7:Business units`,
    },
    {
      index: 8,
      name: $localize`:@@stagesName8:Scale & Adoption`,
      whatHappens: $localize`:@@stagesWhatHappens8:What worked gets rolled out properly — so the improvement reaches everyone it should.`,
      whatYouNeed: $localize`:@@stagesWhatYouNeed8:See your idea's impact on strategic objectives.`,
      owner: $localize`:@@stagesOwner8:Impact measurement team`,
    },
  ];
}

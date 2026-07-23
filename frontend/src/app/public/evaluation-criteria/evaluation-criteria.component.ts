import { Component } from '@angular/core';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';

interface EvaluationCriterion {
  name: string;
  weight: string;
  desc: string;
}

@Component({
  selector: 'app-evaluation-criteria',
  imports: [PublicPageHeroComponent],
  templateUrl: './evaluation-criteria.component.html',
})
export class EvaluationCriteriaComponent {
  readonly pageTitle = $localize`:@@evaluationCriteriaTitle:Evaluation Criteria`;
  readonly pageBody = $localize`:@@evaluationCriteriaBody:How submitted ideas are assessed by evaluators and the committee.`;

  readonly criteria: readonly EvaluationCriterion[] = [
    {
      name: $localize`:@@evaluationCriteriaName1:Strategic Alignment`,
      weight: $localize`:@@evaluationCriteriaWeight1:25%`,
      desc: $localize`:@@evaluationCriteriaDesc1:How closely the idea supports the Authority's strategic themes and national priorities.`,
    },
    {
      name: $localize`:@@evaluationCriteriaName2:Innovation`,
      weight: $localize`:@@evaluationCriteriaWeight2:20%`,
      desc: $localize`:@@evaluationCriteriaDesc2:Originality and the degree to which the idea improves on existing approaches.`,
    },
    {
      name: $localize`:@@evaluationCriteriaName3:Feasibility`,
      weight: $localize`:@@evaluationCriteriaWeight3:20%`,
      desc: $localize`:@@evaluationCriteriaDesc3:Technical and operational practicality within available resources and timeframe.`,
    },
    {
      name: $localize`:@@evaluationCriteriaName4:Impact`,
      weight: $localize`:@@evaluationCriteriaWeight4:25%`,
      desc: $localize`:@@evaluationCriteriaDesc4:Expected scale and significance of the outcome if implemented.`,
    },
    {
      name: $localize`:@@evaluationCriteriaName5:Effort`,
      weight: $localize`:@@evaluationCriteriaWeight5:10%`,
      desc: $localize`:@@evaluationCriteriaDesc5:Cost, complexity, and effort required relative to the expected benefit.`,
    },
  ];

  readonly footerNote = $localize`:@@evaluationCriteriaFooterNote:Each evaluator scores 1 to 5 per criterion. Strong ideas advance automatically; borderline ones go to the committee.`;
}

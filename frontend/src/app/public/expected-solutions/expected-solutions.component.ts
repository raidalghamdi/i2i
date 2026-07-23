import { Component } from '@angular/core';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';
import { IconComponent } from '../../shared/icon/icon.component';

interface SolutionCategory {
  title: string;
  items: readonly string[];
}

@Component({
  selector: 'app-expected-solutions',
  imports: [PublicPageHeroComponent, IconComponent],
  templateUrl: './expected-solutions.component.html',
})
export class ExpectedSolutionsComponent {
  readonly pageTitle = $localize`:@@expectedSolutionsTitle:Solutions We Are Looking For`;
  readonly pageBody = $localize`:@@expectedSolutionsBody:Examples of the kinds of solutions the program seeks across priority areas.`;

  readonly categories: readonly SolutionCategory[] = [
    {
      title: $localize`:@@expectedSolutionsCat1Title:Market Monitoring & Analytics`,
      items: [
        $localize`:@@expectedSolutionsCat1Item1:Data platforms to detect anti-competitive behavior`,
        $localize`:@@expectedSolutionsCat1Item2:Price and market-concentration dashboards`,
        $localize`:@@expectedSolutionsCat1Item3:Automated market-signal alerts`,
      ],
    },
    {
      title: $localize`:@@expectedSolutionsCat2Title:Regulatory Efficiency`,
      items: [
        $localize`:@@expectedSolutionsCat2Item1:Faster merger-review workflows`,
        $localize`:@@expectedSolutionsCat2Item2:Self-service compliance tools for businesses`,
        $localize`:@@expectedSolutionsCat2Item3:Smart case-management systems`,
      ],
    },
    {
      title: $localize`:@@expectedSolutionsCat3Title:Consumer & Business Awareness`,
      items: [
        $localize`:@@expectedSolutionsCat3Item1:Awareness campaigns and channels`,
        $localize`:@@expectedSolutionsCat3Item2:Reporting tools for consumers`,
        $localize`:@@expectedSolutionsCat3Item3:Educational content for SMEs`,
      ],
    },
    {
      title: $localize`:@@expectedSolutionsCat4Title:Emerging Technology`,
      items: [
        $localize`:@@expectedSolutionsCat4Item1:AI for document and case analysis`,
        $localize`:@@expectedSolutionsCat4Item2:Predictive models for market risk`,
        $localize`:@@expectedSolutionsCat4Item3:Secure data-sharing between agencies`,
      ],
    },
  ];
}

import { Component } from '@angular/core';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { FinalRankingPanelComponent } from '../../shared/final-ranking-panel/final-ranking-panel.component';

@Component({
  selector: 'app-final-ranking-page',
  imports: [PageHeaderComponent, FinalRankingPanelComponent],
  templateUrl: './final-ranking-page.component.html',
})
export class FinalRankingPageComponent {}

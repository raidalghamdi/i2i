import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../shared/icon/icon.component';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { FinalRankingPanelComponent } from '../../shared/final-ranking-panel/final-ranking-panel.component';

@Component({
  selector: 'app-supervisor-dashboard',
  imports: [RouterLink, IconComponent, PageHeaderComponent, FinalRankingPanelComponent],
  templateUrl: './supervisor-dashboard.component.html',
})
export class SupervisorDashboardComponent {}

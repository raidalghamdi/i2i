import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../shared/icon/icon.component';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';

@Component({
  selector: 'app-cms-dashboard',
  imports: [RouterLink, IconComponent, PageHeaderComponent],
  templateUrl: './cms-dashboard.component.html',
})
export class CmsDashboardComponent {}

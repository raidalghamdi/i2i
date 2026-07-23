import { Component } from '@angular/core';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';
import { IconComponent } from '../../shared/icon/icon.component';

@Component({
  selector: 'app-partners',
  imports: [PublicPageHeroComponent, IconComponent],
  templateUrl: './partners.component.html',
})
export class PartnersComponent {
  readonly pageTitle = $localize`:@@partnersTitle:Our Partners`;
  readonly pageBody = $localize`:@@partnersBody:We collaborate with entities across government, academia, and the private sector.`;

  readonly partners: readonly string[] = [
    $localize`:@@partnersName1:Ministry of Commerce`,
    $localize`:@@partnersName2:Monsha'at (SME Authority)`,
    $localize`:@@partnersName3:SDAIA`,
    $localize`:@@partnersName4:King Abdulaziz City for Science and Technology`,
    $localize`:@@partnersName5:Saudi Data & AI Authority`,
    $localize`:@@partnersName6:Local Universities`,
    $localize`:@@partnersName7:Private Sector Chambers`,
    $localize`:@@partnersName8:Innovation Hubs`,
  ];
}

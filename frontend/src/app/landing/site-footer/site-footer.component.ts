import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../shared/icon/icon.component';

@Component({
  selector: 'app-site-footer',
  imports: [IconComponent, RouterLink],
  templateUrl: './site-footer.component.html',
})
export class SiteFooterComponent {
  readonly year = new Date().getFullYear();

  readonly socialLinks = [
    { icon: 'linkedin' as const, label: 'LinkedIn', href: 'https://www.linkedin.com/company/gac-ksa' },
    { icon: 'twitter' as const, label: 'X (Twitter)', href: 'https://twitter.com/GAC_KSA' },
    { icon: 'youtube' as const, label: 'YouTube', href: 'https://www.youtube.com/@GAC_KSA' },
  ];
}

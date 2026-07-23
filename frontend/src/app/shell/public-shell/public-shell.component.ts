import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { IdentityService } from '../../core/auth/identity.service';
import { LocaleService } from '../../core/locale.service';
import { IconComponent } from '../../shared/icon/icon.component';
import { SiteFooterComponent } from '../../landing/site-footer/site-footer.component';
import { BackToTopComponent } from '../../landing/back-to-top/back-to-top.component';

@Component({
  selector: 'app-public-shell',
  imports: [RouterLink, RouterOutlet, IconComponent, SiteFooterComponent, BackToTopComponent],
  templateUrl: './public-shell.component.html',
})
export class PublicShellComponent {
  private readonly identityService = inject(IdentityService);
  private readonly localeService = inject(LocaleService);
  readonly identity = this.identityService.identity;

  /** True when a real signed-in identity is present (offer a workboard link instead of login). */
  readonly isSignedIn = computed(() => (this.identity()?.roles.length ?? 0) > 0);

  alternateLocaleHref(): string {
    return this.localeService.alternateLocaleHref();
  }
}

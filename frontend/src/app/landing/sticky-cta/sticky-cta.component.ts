import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IdentityService } from '../../core/auth/identity.service';
import { RoleCodes } from '../../core/auth/role-codes';
import { IconComponent } from '../../shared/icon/icon.component';

const IDEA_AUTHOR_ROLES: readonly string[] = [RoleCodes.Submitter, RoleCodes.Admin];

@Component({
  selector: 'app-sticky-cta',
  imports: [RouterLink, IconComponent],
  templateUrl: './sticky-cta.component.html',
})
export class StickyCtaComponent {
  private readonly identityService = inject(IdentityService);
  private readonly scrolledPastThreshold = signal(false);

  readonly visible = computed(() => {
    const activeRole = this.identityService.identity()?.activeRole;
    return this.scrolledPastThreshold() && !!activeRole && IDEA_AUTHOR_ROLES.includes(activeRole);
  });

  @HostListener('window:scroll')
  onScroll(): void {
    this.scrolledPastThreshold.set(window.scrollY > 600);
  }
}

import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IdentityService } from '../../core/auth/identity.service';
import { IconComponent } from '../../shared/icon/icon.component';

@Component({
  selector: 'app-sticky-cta',
  imports: [RouterLink, IconComponent],
  templateUrl: './sticky-cta.component.html',
})
export class StickyCtaComponent {
  private readonly identityService = inject(IdentityService);
  private readonly scrolledPastThreshold = signal(false);

  readonly visible = computed(() => this.scrolledPastThreshold() && this.identityService.identity()?.activeRole !== 'admin');

  @HostListener('window:scroll')
  onScroll(): void {
    this.scrolledPastThreshold.set(window.scrollY > 600);
  }
}

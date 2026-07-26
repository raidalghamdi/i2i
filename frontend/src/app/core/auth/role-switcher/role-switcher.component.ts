import { Component, inject } from '@angular/core';
import { IdentityService } from '../identity.service';

@Component({
  selector: 'app-role-switcher',
  templateUrl: './role-switcher.component.html',
})
export class RoleSwitcherComponent {
  private readonly identityService = inject(IdentityService);
  readonly identity = this.identityService.identity;

  onChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.identityService.setActiveRole(value);
  }
}

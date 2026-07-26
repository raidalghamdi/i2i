import { Component } from '@angular/core';
import { environment } from '../../../../environments/environment';

/**
 * Dev-only: a dropdown for switching the impersonated user. Writes the chosen sam-account name
 * to localStorage['devUser'] and reloads so the devUserInterceptor re-sends X-Dev-User and the
 * app re-fetches identity as that user. Independent of the role switcher. Hidden in production.
 */
@Component({
  selector: 'app-dev-user-switcher',
  templateUrl: './dev-user-switcher.component.html',
})
export class DevUserSwitcherComponent {
  /** Never render in production — the impersonation header is a no-op there anyway. */
  readonly enabled = !environment.production;
  /** The currently impersonated user (localStorage override, falling back to the build-time default). */
  readonly current = this.readCurrent();
  /** Build-time list, with the active user prepended if it's an off-list value set manually. */
  readonly users = this.buildUserList();

  onChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    try {
      localStorage.setItem('devUser', value);
    } catch {
      // localStorage unavailable (SSR / restricted) — can't persist, so skip the reload.
      return;
    }
    this.reload();
  }

  /** Extracted so tests can stub the page reload. */
  protected reload(): void {
    location.reload();
  }

  private readCurrent(): string {
    try {
      const override = localStorage.getItem('devUser');
      if (override && override.trim().length > 0) {
        return override.trim();
      }
    } catch {
      // fall through to the build-time default
    }
    return environment.devUser;
  }

  private buildUserList(): string[] {
    const list = [...environment.devUsers];
    if (this.current && !list.includes(this.current)) {
      list.unshift(this.current);
    }
    return list;
  }
}

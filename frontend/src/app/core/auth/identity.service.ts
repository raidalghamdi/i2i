import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { IdentityResponse, IdentityState } from './identity.model';

const ACTIVE_ROLE_KEY = 'activeRole';

@Injectable({ providedIn: 'root' })
export class IdentityService {
  private readonly http = inject(HttpClient);
  private readonly state = signal<IdentityState | null>(null);
  private readonly failed = signal(false);

  readonly identity = this.state.asReadonly();
  readonly loadFailed = this.failed.asReadonly();

  async load(): Promise<void> {
    this.failed.set(false);
    try {
      const response = await firstValueFrom(this.http.get<IdentityResponse>('/api/identity/me'));
      const storedRole = localStorage.getItem(ACTIVE_ROLE_KEY);
      const activeRole = storedRole && response.roles.includes(storedRole)
        ? storedRole
        : (response.roles[0] ?? null);
      this.state.set({ ...response, activeRole });
    } catch {
      this.failed.set(true);
    }
  }

  setActiveRole(role: string): void {
    const current = this.state();
    if (!current || !current.roles.includes(role)) {
      return;
    }
    localStorage.setItem(ACTIVE_ROLE_KEY, role);
    this.state.set({ ...current, activeRole: role });
  }
}

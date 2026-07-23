import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface LoginResult {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: { id: string; email: string; fullNameEn: string; roles: string[] };
}

export interface RefreshResult {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

/** Calls the JWT auth endpoints added for the Staging (cloud) deployment -- see backend Api/Auth/AuthEndpoints.cs. */
@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);

  login(email: string, password: string): Promise<LoginResult> {
    return firstValueFrom(this.http.post<LoginResult>('/api/auth/login', { email, password }));
  }

  refresh(refreshToken: string): Promise<RefreshResult> {
    return firstValueFrom(this.http.post<RefreshResult>('/api/auth/refresh', { refreshToken }));
  }

  logout(refreshToken: string): Promise<{ ok: boolean }> {
    return firstValueFrom(this.http.post<{ ok: boolean }>('/api/auth/logout', { refreshToken }));
  }

  changePassword(currentPassword: string, newPassword: string): Promise<{ ok: boolean }> {
    return firstValueFrom(
      this.http.post<{ ok: boolean }>('/api/auth/change-password', { currentPassword, newPassword }),
    );
  }
}

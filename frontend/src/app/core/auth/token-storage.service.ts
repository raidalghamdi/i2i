import { Injectable } from '@angular/core';

export interface StoredTokens {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

const ACCESS_TOKEN_KEY = 'i2i-access-token';
const REFRESH_TOKEN_KEY = 'i2i-refresh-token';
const EXPIRES_AT_KEY = 'i2i-token-expires-at';

/**
 * Persists the JWT access/refresh token pair across page loads (localStorage), so a signed-in
 * session survives closing the tab -- the same "stay signed in" experience DevAuth/Negotiate give
 * for free via the browser's own session, which JWT has to replicate explicitly.
 */
@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  set(tokens: StoredTokens): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
    localStorage.setItem(EXPIRES_AT_KEY, tokens.expiresAt);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  /** True once we're within 30s of expiry (or already past it) -- refresh proactively rather than race the server. */
  isAccessTokenExpiringSoon(): boolean {
    const expiresAt = localStorage.getItem(EXPIRES_AT_KEY);
    if (!expiresAt) return true;
    return new Date(expiresAt).getTime() - Date.now() < 30_000;
  }

  hasSession(): boolean {
    return this.getRefreshToken() !== null;
  }

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(EXPIRES_AT_KEY);
  }
}

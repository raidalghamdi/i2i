import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Subject, catchError, filter, from, switchMap, take, throwError } from 'rxjs';
import { AuthApiService } from './auth-api.service';
import { TokenStorageService } from './token-storage.service';

const AUTH_ENDPOINTS = ['/api/auth/login', '/api/auth/refresh'];

// Module-scoped (not per-request) so concurrent 401s share a single in-flight refresh instead of
// each firing their own -- interceptor functions live for the app's lifetime, same as a singleton
// service, so this is safe state to hold here.
let refreshInFlight = false;
const refreshed$ = new Subject<string>();

/**
 * Attaches the JWT access token to API calls and transparently refreshes it when needed --
 * proactively when it's about to expire, reactively on a 401 as a fallback. No-op when there's no
 * stored session (Negotiate/DevAuth requests pass through unchanged, same as before this existed).
 */
export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/') || AUTH_ENDPOINTS.some((p) => req.url.startsWith(p))) {
    return next(req);
  }

  const tokenStorage = inject(TokenStorageService);
  const authApi = inject(AuthApiService);

  const accessToken = tokenStorage.getAccessToken();
  if (!accessToken) {
    return next(req);
  }

  const withAuth = (token: string) => req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });

  const doRefresh = () => {
    const refreshToken = tokenStorage.getRefreshToken();
    if (!refreshToken) {
      tokenStorage.clear();
      return throwError(() => new Error('No refresh token available'));
    }

    if (refreshInFlight) {
      return refreshed$.pipe(
        take(1),
        switchMap((newAccessToken) => next(withAuth(newAccessToken))),
      );
    }

    refreshInFlight = true;
    return from(authApi.refresh(refreshToken)).pipe(
      switchMap((result) => {
        tokenStorage.set(result);
        refreshInFlight = false;
        refreshed$.next(result.accessToken);
        return next(withAuth(result.accessToken));
      }),
      catchError((err) => {
        refreshInFlight = false;
        tokenStorage.clear();
        return throwError(() => err);
      }),
    );
  };

  if (tokenStorage.isAccessTokenExpiringSoon()) {
    return doRefresh();
  }

  return next(withAuth(accessToken)).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse && err.status === 401) {
        return doRefresh();
      }
      return throwError(() => err);
    }),
  );
};

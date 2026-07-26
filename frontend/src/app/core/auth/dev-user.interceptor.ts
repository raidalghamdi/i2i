import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';

/**
 * Dev-only: picks the impersonated user for the X-Dev-User header. Reads an override from
 * localStorage['devUser'] first so you can switch users in the browser without a rebuild —
 * e.g. `localStorage.setItem('devUser', 'submitter3'); location.reload();` — falling back to
 * the build-time environment.devUser. No effect in production.
 */
export const devUserInterceptor: HttpInterceptorFn = (req, next) => {
  if (environment.production) {
    return next(req);
  }
  let devUser = environment.devUser;
  try {
    const override = localStorage.getItem('devUser');
    if (override && override.trim().length > 0) {
      devUser = override.trim();
    }
  } catch {
    // localStorage may be unavailable (SSR / restricted contexts) — fall back to environment.devUser.
  }
  return next(req.clone({ setHeaders: { 'X-Dev-User': devUser } }));
};

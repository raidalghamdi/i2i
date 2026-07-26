import { HttpEvent, HttpHandlerFn, HttpRequest } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { devUserInterceptor } from './dev-user.interceptor';

describe('devUserInterceptor', () => {
  const originalProduction = environment.production;

  beforeEach(() => {
    localStorage.removeItem('devUser');
  });

  afterEach(() => {
    (environment as { production: boolean }).production = originalProduction;
    localStorage.removeItem('devUser');
  });

  it('adds the X-Dev-User header when not in production', () => {
    (environment as { production: boolean }).production = false;
    const req = new HttpRequest('GET', '/api/identity/me');
    let captured: HttpRequest<unknown> | undefined;
    const next: HttpHandlerFn = (r) => {
      captured = r;
      return of({} as HttpEvent<unknown>);
    };

    TestBed.runInInjectionContext(() => devUserInterceptor(req, next));

    expect(captured?.headers.get('X-Dev-User')).toBe(environment.devUser);
  });

  it('uses the localStorage devUser override when set', () => {
    (environment as { production: boolean }).production = false;
    localStorage.setItem('devUser', 'judge3');
    const req = new HttpRequest('GET', '/api/identity/me');
    let captured: HttpRequest<unknown> | undefined;
    const next: HttpHandlerFn = (r) => {
      captured = r;
      return of({} as HttpEvent<unknown>);
    };

    TestBed.runInInjectionContext(() => devUserInterceptor(req, next));

    expect(captured?.headers.get('X-Dev-User')).toBe('judge3');
  });

  it('does not add the header in production', () => {
    (environment as { production: boolean }).production = true;
    const req = new HttpRequest('GET', '/api/identity/me');
    let captured: HttpRequest<unknown> | undefined;
    const next: HttpHandlerFn = (r) => {
      captured = r;
      return of({} as HttpEvent<unknown>);
    };

    TestBed.runInInjectionContext(() => devUserInterceptor(req, next));

    expect(captured?.headers.has('X-Dev-User')).toBe(false);
  });
});

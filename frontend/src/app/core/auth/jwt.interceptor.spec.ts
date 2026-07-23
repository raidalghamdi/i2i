import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpRequest } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { jwtInterceptor } from './jwt.interceptor';
import { AuthApiService } from './auth-api.service';
import { TokenStorageService } from './token-storage.service';

describe('jwtInterceptor', () => {
  let authApiSpy: jasmine.SpyObj<AuthApiService>;
  let tokenStorage: TokenStorageService;

  beforeEach(() => {
    localStorage.clear();
    authApiSpy = jasmine.createSpyObj('AuthApiService', ['refresh']);
    TestBed.configureTestingModule({
      providers: [{ provide: AuthApiService, useValue: authApiSpy }],
    });
    tokenStorage = TestBed.inject(TokenStorageService);
  });

  afterEach(() => localStorage.clear());

  function run(req: HttpRequest<unknown>, next: HttpHandlerFn) {
    return TestBed.runInInjectionContext(() => firstValueFrom(jwtInterceptor(req, next) as any));
  }

  it('passes requests through unchanged when there is no stored session', async () => {
    const req = new HttpRequest('GET', '/api/identity/me');
    let captured: HttpRequest<unknown> | undefined;
    const next: HttpHandlerFn = (r) => {
      captured = r;
      return of({} as HttpEvent<unknown>);
    };

    await run(req, next);

    expect(captured?.headers.has('Authorization')).toBe(false);
    expect(authApiSpy.refresh).not.toHaveBeenCalled();
  });

  it('never touches /api/auth/login or /api/auth/refresh requests themselves', async () => {
    tokenStorage.set({ accessToken: 'a', refreshToken: 'r', expiresAt: new Date(Date.now() + 60_000).toISOString() });
    const req = new HttpRequest('POST', '/api/auth/login', { email: 'x', password: 'y' });
    let captured: HttpRequest<unknown> | undefined;
    const next: HttpHandlerFn = (r) => {
      captured = r;
      return of({} as HttpEvent<unknown>);
    };

    await run(req, next);

    expect(captured?.headers.has('Authorization')).toBe(false);
  });

  it('attaches the access token when it is not close to expiring', async () => {
    tokenStorage.set({ accessToken: 'valid-token', refreshToken: 'r', expiresAt: new Date(Date.now() + 5 * 60_000).toISOString() });
    const req = new HttpRequest('GET', '/api/identity/me');
    let captured: HttpRequest<unknown> | undefined;
    const next: HttpHandlerFn = (r) => {
      captured = r;
      return of({} as HttpEvent<unknown>);
    };

    await run(req, next);

    expect(captured?.headers.get('Authorization')).toBe('Bearer valid-token');
    expect(authApiSpy.refresh).not.toHaveBeenCalled();
  });

  it('proactively refreshes and retries with the new token when the stored token is about to expire', async () => {
    tokenStorage.set({ accessToken: 'stale', refreshToken: 'r-1', expiresAt: new Date(Date.now() + 5_000).toISOString() });
    authApiSpy.refresh.and.returnValue(
      Promise.resolve({ accessToken: 'fresh', refreshToken: 'r-2', expiresAt: new Date(Date.now() + 60_000).toISOString() }),
    );
    const req = new HttpRequest('GET', '/api/identity/me');
    let captured: HttpRequest<unknown> | undefined;
    const next: HttpHandlerFn = (r) => {
      captured = r;
      return of({} as HttpEvent<unknown>);
    };

    await run(req, next);

    expect(authApiSpy.refresh).toHaveBeenCalledWith('r-1');
    expect(captured?.headers.get('Authorization')).toBe('Bearer fresh');
    expect(tokenStorage.getAccessToken()).toBe('fresh');
  });

  it('reactively refreshes and retries once on a 401, then succeeds', async () => {
    tokenStorage.set({ accessToken: 'expired-server-side', refreshToken: 'r-1', expiresAt: new Date(Date.now() + 5 * 60_000).toISOString() });
    authApiSpy.refresh.and.returnValue(
      Promise.resolve({ accessToken: 'fresh', refreshToken: 'r-2', expiresAt: new Date(Date.now() + 60_000).toISOString() }),
    );
    const req = new HttpRequest('GET', '/api/identity/me');
    let callCount = 0;
    const next: HttpHandlerFn = (r) => {
      callCount++;
      if (callCount === 1) {
        return throwError(() => new HttpErrorResponse({ status: 401 }));
      }
      expect(r.headers.get('Authorization')).toBe('Bearer fresh');
      return of({} as HttpEvent<unknown>);
    };

    await run(req, next);

    expect(callCount).toBe(2);
    expect(authApiSpy.refresh).toHaveBeenCalledTimes(1);
  });

  it('clears the session and propagates the error when refresh itself fails', async () => {
    tokenStorage.set({ accessToken: 'expired', refreshToken: 'dead-refresh-token', expiresAt: new Date(Date.now() + 5 * 60_000).toISOString() });
    authApiSpy.refresh.and.returnValue(Promise.reject(new HttpErrorResponse({ status: 401 })));
    const req = new HttpRequest('GET', '/api/identity/me');
    const next: HttpHandlerFn = () => throwError(() => new HttpErrorResponse({ status: 401 }));

    await expectAsync(run(req, next)).toBeRejected();

    expect(tokenStorage.getAccessToken()).toBeNull();
    expect(tokenStorage.hasSession()).toBe(false);
  });
});

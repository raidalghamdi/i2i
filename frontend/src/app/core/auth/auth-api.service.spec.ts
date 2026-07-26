import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AuthApiService } from './auth-api.service';

describe('AuthApiService', () => {
  let service: AuthApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('posts email/password to /api/auth/login', async () => {
    const promise = service.login('a@b.com', 'pw');
    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'a@b.com', password: 'pw' });
    req.flush({ accessToken: 'a', refreshToken: 'r', expiresAt: '2026-01-01', user: { id: '1', email: 'a@b.com', fullNameEn: 'A', roles: [] } });
    await expectAsync(promise).toBeResolved();
  });

  it('posts refreshToken to /api/auth/refresh', async () => {
    const promise = service.refresh('r-1');
    const req = httpMock.expectOne('/api/auth/refresh');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ refreshToken: 'r-1' });
    req.flush({ accessToken: 'a2', refreshToken: 'r2', expiresAt: '2026-01-01' });
    await expectAsync(promise).toBeResolved();
  });

  it('posts refreshToken to /api/auth/logout', async () => {
    const promise = service.logout('r-1');
    const req = httpMock.expectOne('/api/auth/logout');
    expect(req.request.body).toEqual({ refreshToken: 'r-1' });
    req.flush({ ok: true });
    await expectAsync(promise).toBeResolved();
  });

  it('posts current/new password to /api/auth/change-password', async () => {
    const promise = service.changePassword('old', 'new');
    const req = httpMock.expectOne('/api/auth/change-password');
    expect(req.request.body).toEqual({ currentPassword: 'old', newPassword: 'new' });
    req.flush({ ok: true });
    await expectAsync(promise).toBeResolved();
  });
});

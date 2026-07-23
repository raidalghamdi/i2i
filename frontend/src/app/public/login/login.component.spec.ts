import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { LoginComponent } from './login.component';
import { TokenStorageService } from '../../core/auth/token-storage.service';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let httpMock: HttpTestingController;
  let tokenStorage: TokenStorageService;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    fixture = TestBed.createComponent(LoginComponent);
    httpMock = TestBed.inject(HttpTestingController);
    tokenStorage = TestBed.inject(TokenStorageService);
    router = TestBed.inject(Router);
    spyOn(router, 'navigateByUrl').and.resolveTo(true);
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('does not submit when the form is invalid', () => {
    fixture.componentInstance.onSubmit();

    httpMock.expectNone('/api/auth/login');
    expect(fixture.componentInstance.form.controls.email.touched).toBe(true);
  });

  it('stores tokens, loads identity, and navigates to the dashboard on success', async () => {
    fixture.componentInstance.form.setValue({ email: 'admin@internal.sa', password: 'pw' });

    fixture.componentInstance.onSubmit();

    const loginReq = httpMock.expectOne('/api/auth/login');
    expect(loginReq.request.method).toBe('POST');
    loginReq.flush({
      accessToken: 'access-1',
      refreshToken: 'refresh-1',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: { id: '1', email: 'admin@internal.sa', fullNameEn: 'Admin', roles: ['admin'] },
    });

    // Two microtask ticks: one for the login firstValueFrom() to resolve, one for the subsequent
    // `await this.identityService.load()` in onSubmit() to actually dispatch its HTTP request.
    await Promise.resolve();
    await Promise.resolve();

    const identityReq = httpMock.expectOne('/api/identity/me');
    identityReq.flush({ samAccountName: 'admin.bootstrap', email: 'admin@internal.sa', department: null, roles: ['admin'] });

    await fixture.whenStable();

    expect(tokenStorage.getAccessToken()).toBe('access-1');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/dashboard');
  });

  it('shows an error message and does not store tokens when login fails', async () => {
    fixture.componentInstance.form.setValue({ email: 'admin@internal.sa', password: 'wrong' });

    fixture.componentInstance.onSubmit();

    const req = httpMock.expectOne('/api/auth/login');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBeTruthy();
    expect(tokenStorage.getAccessToken()).toBeNull();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });
});

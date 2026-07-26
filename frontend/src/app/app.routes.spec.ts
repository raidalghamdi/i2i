import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideRouter, Router } from '@angular/router';
import { routes } from './app.routes';
import { ACCESS_PASSWORD_HASH, ACCESS_TOKEN_KEY } from './core/access/access'; // Change 20260726
import { IdentityService } from './core/auth/identity.service';
import { PublicShellComponent } from './shell/public-shell/public-shell.component';
import { AppShellComponent } from './shell/app-shell/app-shell.component';

describe('app.routes (public/app shell reparent)', () => {
  // Change 20260726 — every top-level route now sits behind the shared testing gate, so
  // these shell/guard assertions need the access token in place first.
  beforeEach(() => localStorage.setItem(ACCESS_TOKEN_KEY, ACCESS_PASSWORD_HASH)); // Change 20260726
  afterEach(() => localStorage.removeItem(ACCESS_TOKEN_KEY)); // Change 20260726

  async function setup(identityOverride: unknown) {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: IdentityService,
          useValue: {
            identity: signal(identityOverride),
            loadFailed: signal(false),
          },
        },
      ],
    });
    return RouterTestingHarness.create();
  }

  it('resolves the empty path under PublicShellComponent and renders the landing page', async () => {
    const harness = await setup(null);
    const rootComponent = await harness.navigateByUrl('/');

    expect(rootComponent).toBeInstanceOf(PublicShellComponent);
    const el = harness.routeNativeElement as HTMLElement;
    expect(el.tagName.toLowerCase()).toBe('app-public-shell');
    expect(el.querySelector('app-landing')).not.toBeNull();
  });

  it('resolves an authenticated path under AppShellComponent when the role guard passes', async () => {
    const harness = await setup({
      samAccountName: 'submitter1',
      email: null,
      department: 'Innovation',
      roles: ['submitter'],
      activeRole: 'submitter',
    });
    const rootComponent = await harness.navigateByUrl('/dashboard');

    const router = TestBed.inject(Router);
    expect(router.url).toBe('/dashboard');
    expect(rootComponent).toBeInstanceOf(AppShellComponent);
    const el = harness.routeNativeElement as HTMLElement;
    expect(el.tagName.toLowerCase()).toBe('app-app-shell');
  });

  it('does NOT fall through to AppShellComponent for an authed path when the guard fails (redirects to public shell)', async () => {
    const harness = await setup(null);
    const rootComponent = await harness.navigateByUrl('/dashboard');

    const router = TestBed.inject(Router);
    expect(router.url).toBe('/');
    expect(rootComponent).toBeInstanceOf(PublicShellComponent);
  });
});

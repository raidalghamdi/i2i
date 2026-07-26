import { Component } from '@angular/core'; // Change 20260726
import { TestBed } from '@angular/core/testing'; // Change 20260726
import { provideRouter, Router } from '@angular/router'; // Change 20260726
import { RouterTestingHarness } from '@angular/router/testing'; // Change 20260726
import { ACCESS_PASSWORD_HASH, ACCESS_TOKEN_KEY, sha256Hex } from './access'; // Change 20260726
import { accessGateGuard, accessLogoutGuard } from './access-gate.guard'; // Change 20260726
import { AccessGateComponent } from './access-gate.component'; // Change 20260726

/** The real shared password; only ever present in test code, never in the shipped bundle. */ // Change 20260726
const PASSWORD = 'Demo2026!@#$'; // Change 20260726

@Component({ selector: 'app-target-stub', template: 'target' }) // Change 20260726
class TargetStubComponent {} // Change 20260726

function configure(): void { // Change 20260726
  TestBed.configureTestingModule({ // Change 20260726
    providers: [ // Change 20260726
      provideRouter([ // Change 20260726
        { path: 'gate', component: AccessGateComponent }, // Change 20260726
        { path: 'access-logout', canActivate: [accessLogoutGuard], component: AccessGateComponent }, // Change 20260726
        { path: 'target', component: TargetStubComponent, canActivate: [accessGateGuard] }, // Change 20260726
      ]), // Change 20260726
    ], // Change 20260726
  }); // Change 20260726
} // Change 20260726

describe('access gate', () => { // Change 20260726
  beforeEach(() => { // Change 20260726
    localStorage.removeItem(ACCESS_TOKEN_KEY); // Change 20260726
    configure(); // Change 20260726
  }); // Change 20260726

  afterEach(() => localStorage.removeItem(ACCESS_TOKEN_KEY)); // Change 20260726

  it('publishes a hash that matches the shared password', async () => { // Change 20260726
    expect(await sha256Hex(PASSWORD)).toBe(ACCESS_PASSWORD_HASH); // Change 20260726
  }); // Change 20260726

  it('stores the token and lands on the requested route for the correct password', async () => { // Change 20260726
    const harness = await RouterTestingHarness.create(); // Change 20260726
    const gate = await harness.navigateByUrl('/gate?next=/target', AccessGateComponent); // Change 20260726

    gate.password.set(PASSWORD); // Change 20260726
    await gate.submit(); // Change 20260726

    expect(localStorage.getItem(ACCESS_TOKEN_KEY)).toBe(ACCESS_PASSWORD_HASH); // Change 20260726
    expect(TestBed.inject(Router).url).toBe('/target'); // Change 20260726
    expect(gate.incorrect()).toBe(false); // Change 20260726
  }); // Change 20260726

  it('stores nothing and shows an error for a wrong password', async () => { // Change 20260726
    const harness = await RouterTestingHarness.create(); // Change 20260726
    const gate = await harness.navigateByUrl('/gate', AccessGateComponent); // Change 20260726

    gate.password.set('not-the-password'); // Change 20260726
    await gate.submit(); // Change 20260726
    harness.detectChanges(); // Change 20260726

    expect(localStorage.getItem(ACCESS_TOKEN_KEY)).toBeNull(); // Change 20260726
    expect(gate.incorrect()).toBe(true); // Change 20260726
    expect(TestBed.inject(Router).url).toBe('/gate'); // Change 20260726

    const alert = (harness.routeNativeElement as HTMLElement).querySelector('[role="alert"]'); // Change 20260726
    expect(alert?.textContent).toContain('Incorrect password'); // Change 20260726
    expect(alert?.textContent).toContain('كلمة السر غير صحيحة'); // Change 20260726
  }); // Change 20260726

  it('sends a locked visitor to the gate, remembering where they were headed', async () => { // Change 20260726
    const harness = await RouterTestingHarness.create(); // Change 20260726
    await harness.navigateByUrl('/target'); // Change 20260726

    const router = TestBed.inject(Router); // Change 20260726
    expect(router.url.startsWith('/gate')).toBe(true); // Change 20260726
    expect(router.parseUrl(router.url).queryParams['next']).toBe('/target'); // Change 20260726
  }); // Change 20260726

  it('lets an already-unlocked visitor straight through', async () => { // Change 20260726
    localStorage.setItem(ACCESS_TOKEN_KEY, ACCESS_PASSWORD_HASH); // Change 20260726
    const harness = await RouterTestingHarness.create(); // Change 20260726
    await harness.navigateByUrl('/target'); // Change 20260726

    expect(TestBed.inject(Router).url).toBe('/target'); // Change 20260726
  }); // Change 20260726

  it('unlocks from ?access= and strips the password out of the URL', async () => { // Change 20260726
    const harness = await RouterTestingHarness.create(); // Change 20260726
    await harness.navigateByUrl(`/target?access=${encodeURIComponent(PASSWORD)}&keep=1`); // Change 20260726

    expect(localStorage.getItem(ACCESS_TOKEN_KEY)).toBe(ACCESS_PASSWORD_HASH); // Change 20260726
    expect(TestBed.inject(Router).url).toBe('/target?keep=1'); // Change 20260726
  }); // Change 20260726

  it('falls back to the gate when ?access= carries the wrong password', async () => { // Change 20260726
    const harness = await RouterTestingHarness.create(); // Change 20260726
    await harness.navigateByUrl('/target?access=wrong'); // Change 20260726

    expect(localStorage.getItem(ACCESS_TOKEN_KEY)).toBeNull(); // Change 20260726
    expect(TestBed.inject(Router).url.startsWith('/gate')).toBe(true); // Change 20260726
  }); // Change 20260726

  it('/access-logout clears the token and returns to the gate', async () => { // Change 20260726
    localStorage.setItem(ACCESS_TOKEN_KEY, ACCESS_PASSWORD_HASH); // Change 20260726
    const harness = await RouterTestingHarness.create(); // Change 20260726
    await harness.navigateByUrl('/access-logout'); // Change 20260726

    expect(localStorage.getItem(ACCESS_TOKEN_KEY)).toBeNull(); // Change 20260726
    expect(TestBed.inject(Router).url).toBe('/gate'); // Change 20260726
  }); // Change 20260726
}); // Change 20260726

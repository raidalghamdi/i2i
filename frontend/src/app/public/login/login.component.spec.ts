import { ComponentFixture, TestBed } from '@angular/core/testing'; // Change 20260726
import { provideHttpClient } from '@angular/common/http'; // Change 20260726
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing'; // Change 20260726
import { Router } from '@angular/router'; // Change 20260726
import { LoginComponent } from './login.component'; // Change 20260726

describe('LoginComponent', () => { // Change 20260726
  let fixture: ComponentFixture<LoginComponent>; // Change 20260726
  let httpMock: HttpTestingController; // Change 20260726
  let router: Router; // Change 20260726

  beforeEach(() => { // Change 20260726
    localStorage.clear(); // Change 20260726
    TestBed.configureTestingModule({ // Change 20260726
      imports: [LoginComponent], // Change 20260726
      providers: [provideHttpClient(), provideHttpClientTesting()], // Change 20260726
    }); // Change 20260726

    fixture = TestBed.createComponent(LoginComponent); // Change 20260726
    httpMock = TestBed.inject(HttpTestingController); // Change 20260726
    router = TestBed.inject(Router); // Change 20260726
    spyOn(router, 'navigateByUrl').and.resolveTo(true); // Change 20260726
    fixture.detectChanges(); // Change 20260726
  }); // Change 20260726

  afterEach(() => { // Change 20260726
    httpMock.verify(); // Change 20260726
    localStorage.clear(); // Change 20260726
  }); // Change 20260726

  it('renders no password field', () => { // Change 20260726
    const host = fixture.nativeElement as HTMLElement; // Change 20260726
    expect(host.querySelector('input[type="password"]')).toBeNull(); // Change 20260726
  }); // Change 20260726

  it('resolves the AD identity and navigates to the dashboard', async () => { // Change 20260726
    const signIn = fixture.componentInstance.signInWithGac(); // Change 20260726

    const identityReq = httpMock.expectOne('/api/identity/me'); // Change 20260726
    expect(identityReq.request.method).toBe('GET'); // Change 20260726
    identityReq.flush({ samAccountName: 'admin@internal.sa', email: 'admin@internal.sa', department: null, roles: ['admin'] }); // Change 20260726

    await signIn; // Change 20260726

    expect(router.navigateByUrl).toHaveBeenCalledWith('/dashboard'); // Change 20260726
    expect(fixture.componentInstance.errorMessage()).toBeNull(); // Change 20260726
  }); // Change 20260726

  it('never posts to the retired JWT login endpoint', async () => { // Change 20260726
    const signIn = fixture.componentInstance.signInWithGac(); // Change 20260726

    httpMock.expectNone('/api/auth/login'); // Change 20260726
    httpMock.expectOne('/api/identity/me').flush({ samAccountName: 'admin@internal.sa', email: 'admin@internal.sa', department: null, roles: ['admin'] }); // Change 20260726

    await signIn; // Change 20260726
  }); // Change 20260726

  it('shows an error and stays put when AD identity resolution fails', async () => { // Change 20260726
    const signIn = fixture.componentInstance.signInWithGac(); // Change 20260726

    httpMock.expectOne('/api/identity/me').flush('Unauthorized', { status: 401, statusText: 'Unauthorized' }); // Change 20260726

    await signIn; // Change 20260726
    fixture.detectChanges(); // Change 20260726

    expect(fixture.componentInstance.errorMessage()).toBeTruthy(); // Change 20260726
    expect(router.navigateByUrl).not.toHaveBeenCalled(); // Change 20260726
  }); // Change 20260726
}); // Change 20260726

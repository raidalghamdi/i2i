import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { AppShellComponent } from './app-shell.component';
import { IdentityService } from '../../core/auth/identity.service';
import { NotificationsApiService } from '../../core/notifications-api.service';

describe('AppShellComponent', () => {
  let fixture: ComponentFixture<AppShellComponent>;
  let identityServiceStub: { identity: ReturnType<typeof signal>; loadFailed: ReturnType<typeof signal>; load: jasmine.Spy };

  function setup(overrides: { loadFailed?: boolean; identity?: unknown }) {
    identityServiceStub = {
      identity: signal(overrides.identity ?? null),
      loadFailed: signal(overrides.loadFailed ?? false),
      load: jasmine.createSpy('load').and.returnValue(Promise.resolve()),
    };
    // NotificationStore (providedIn: 'root') is injected eagerly by AppShellComponent
    // and would otherwise reach the real NotificationsApiService, which needs
    // HttpClient. Stub the api so the shell spec doesn't need HTTP plumbing.
    const notificationsApiStub = jasmine.createSpyObj<NotificationsApiService>('NotificationsApiService', ['list', 'markRead', 'markAllRead']);
    notificationsApiStub.list.and.resolveTo([]);
    TestBed.configureTestingModule({
      imports: [AppShellComponent],
      providers: [
        provideRouter([]),
        { provide: IdentityService, useValue: identityServiceStub },
        { provide: NotificationsApiService, useValue: notificationsApiStub },
      ],
    });
    fixture = TestBed.createComponent(AppShellComponent);
    fixture.detectChanges();
  }

  it('shows the unavailable message when loadFailed is true', () => {
    setup({ loadFailed: true });
    expect(fixture.nativeElement.textContent).toContain('Unable to load your identity');
  });

  it('retries the identity load when the "Try again" button is clicked', () => {
    setup({ loadFailed: true });
    const retryButton = fixture.nativeElement.querySelector('button');
    retryButton.click();
    expect(identityServiceStub.load).toHaveBeenCalled();
  });

  it('shows a loading message when identity is null and load has not failed', () => {
    setup({ loadFailed: false, identity: null });
    expect(fixture.nativeElement.textContent).toContain('Loading');
  });

  it('renders the header and router outlet once identity resolves', () => {
    setup({
      loadFailed: false,
      identity: { samAccountName: 'submitter1', email: null, department: 'Innovation', roles: ['submitter'], activeRole: 'submitter' },
    });
    expect(fixture.nativeElement.querySelector('header')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('submitter1');
  });
});

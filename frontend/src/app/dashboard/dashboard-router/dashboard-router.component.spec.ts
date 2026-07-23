import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { DashboardRouterComponent } from './dashboard-router.component';
import { IdentityService } from '../../core/auth/identity.service';
import { IdentityState } from '../../core/auth/identity.model';

@Component({ selector: 'app-dashboard', template: 'INNOVATOR_STUB' })
class StubInnovatorDashboardComponent {}

@Component({ selector: 'app-committee-dashboard', template: 'COMMITTEE_STUB' })
class StubCommitteeDashboardComponent {}

@Component({ selector: 'app-supervisor-kpi-dashboard', template: 'SUPERVISOR_STUB' })
class StubSupervisorKpiDashboardComponent {}

@Component({ selector: 'app-admin-dashboard', template: 'ADMIN_STUB' })
class StubAdminDashboardComponent {}

class IdentityServiceStub {
  private readonly state = signal<IdentityState | null>(null);
  readonly identity = this.state.asReadonly();

  setRole(activeRole: string | null): void {
    this.state.set({ samAccountName: 's1', email: 's1@x.com', department: null, roles: [], activeRole });
  }
}

describe('DashboardRouterComponent', () => {
  let fixture: ComponentFixture<DashboardRouterComponent>;
  let router: jasmine.SpyObj<Router>;

  function setup(activeRole: string | null): void {
    const identity = new IdentityServiceStub();
    identity.setRole(activeRole);
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [DashboardRouterComponent],
      providers: [
        { provide: IdentityService, useValue: identity },
        { provide: Router, useValue: router },
      ],
    });

    TestBed.overrideComponent(DashboardRouterComponent, {
      set: {
        imports: [
          StubInnovatorDashboardComponent,
          StubCommitteeDashboardComponent,
          StubSupervisorKpiDashboardComponent,
          StubAdminDashboardComponent,
        ],
      },
    });

    fixture = TestBed.createComponent(DashboardRouterComponent);
    fixture.detectChanges();
  }

  it('renders the admin dashboard for the admin role', () => {
    setup('admin');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('ADMIN_STUB');
  });

  it('renders the committee dashboard for the judge role', () => {
    setup('judge');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('COMMITTEE_STUB');
  });

  it('renders the supervisor dashboard for the supervisor role', () => {
    setup('supervisor');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('SUPERVISOR_STUB');
  });

  it('renders the innovator dashboard for the submitter role', () => {
    setup('submitter');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('INNOVATOR_STUB');
  });

  it('renders the innovator dashboard by default when there is no active role', () => {
    setup(null);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('INNOVATOR_STUB');
  });

  it('redirects to /evaluator for the evaluator role', () => {
    setup('evaluator');
    expect(router.navigate).toHaveBeenCalledWith(['/evaluator']);
  });
});

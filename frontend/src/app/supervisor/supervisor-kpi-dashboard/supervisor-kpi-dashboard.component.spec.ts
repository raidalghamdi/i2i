import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SupervisorKpiDashboardComponent } from './supervisor-kpi-dashboard.component';
import { DashboardApiService } from '../../core/dashboard-api.service';

class StubApi {
  getSupervisor() {
    return Promise.resolve({
      teamMembers: 5,
      sectorIdeas: 4,
      escalationsAwaitingMe: 1,
      screening: { total: 3, underReview: 1, approved: 1, returned: 0, rejected: 1 },
    });
  }
}

describe('SupervisorKpiDashboardComponent', () => {
  let fixture: ComponentFixture<SupervisorKpiDashboardComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SupervisorKpiDashboardComponent],
      providers: [provideRouter([]), { provide: DashboardApiService, useClass: StubApi }],
    }).compileComponents();
    fixture = TestBed.createComponent(SupervisorKpiDashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('renders supervisor KPIs, screening buckets, and CTAs', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('5');
    expect(el.textContent).toContain('4');
    expect(el.textContent).toContain('1');
    expect(el.textContent).toContain('3');
    expect(el.textContent).toContain('0');

    const hrefs = Array.from(el.querySelectorAll('a')).map((a) => a.getAttribute('href'));
    expect(hrefs.some((h) => h?.includes('/supervisor/screening'))).toBeTrue();
    expect(hrefs.some((h) => h?.includes('/admin/escalations'))).toBeTrue();
  });
});

describe('SupervisorKpiDashboardComponent (loading state)', () => {
  it('shows a loading indicator until the KPI fetch resolves', async () => {
    type SupervisorData = {
      teamMembers: number;
      sectorIdeas: number;
      escalationsAwaitingMe: number;
      screening: { total: number; underReview: number; approved: number; returned: number; rejected: number };
    };
    let resolveFetch!: (value: SupervisorData) => void;
    const pending = new Promise<SupervisorData>((resolve) => {
      resolveFetch = resolve;
    });
    class DeferredStubApi {
      getSupervisor() {
        return pending;
      }
    }

    await TestBed.configureTestingModule({
      imports: [SupervisorKpiDashboardComponent],
      providers: [provideRouter([]), { provide: DashboardApiService, useClass: DeferredStubApi }],
    }).compileComponents();
    const fixture = TestBed.createComponent(SupervisorKpiDashboardComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeTruthy();

    resolveFetch({
      teamMembers: 5,
      sectorIdeas: 4,
      escalationsAwaitingMe: 1,
      screening: { total: 3, underReview: 1, approved: 1, returned: 0, rejected: 1 },
    });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeNull();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('5');
  });
});

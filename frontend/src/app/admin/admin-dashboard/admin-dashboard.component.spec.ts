import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AdminDashboardComponent } from './admin-dashboard.component';
import { DashboardApiService } from '../../core/dashboard-api.service';

class StubApi {
  getAdmin() {
    return Promise.resolve({
      totalUsers: 12,
      activeIdeas: 30,
      pendingEvaluations: 5,
      health: 'Healthy',
    });
  }
}

describe('AdminDashboardComponent', () => {
  let fixture: ComponentFixture<AdminDashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminDashboardComponent],
      providers: [provideRouter([]), { provide: DashboardApiService, useClass: StubApi }],
    }).compileComponents();
    fixture = TestBed.createComponent(AdminDashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('renders the admin KPI tiles', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('12');
    expect(el.textContent).toContain('30');
    expect(el.textContent).toContain('5');
    expect(el.textContent).toContain('Healthy');
  });

  it('renders a link to the user list', () => {
    fixture.detectChanges();

    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.some((a) => a.getAttribute('href') === '/admin/users')).toBe(true);
  });

  it('renders a link to the CMS dashboard', () => {
    fixture.detectChanges();

    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.some((a) => a.getAttribute('href') === '/admin/cms')).toBe(true);
  });

  it('renders a link to the challenges list', () => {
    fixture.detectChanges();

    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.some((a) => a.getAttribute('href') === '/admin/challenges')).toBe(true);
  });

  it('renders a link to the escalation board', () => {
    fixture.detectChanges();

    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.some((a) => a.getAttribute('href') === '/admin/escalations')).toBe(true);
  });

  it('renders a link to the analytics dashboard', () => {
    fixture.detectChanges();

    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.some((a) => a.getAttribute('href') === '/admin/analytics')).toBe(true);
  });

  it('renders a link to the reports dashboard', () => {
    fixture.detectChanges();

    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.some((a) => a.getAttribute('href') === '/admin/reports')).toBe(true);
  });
});

describe('AdminDashboardComponent (loading state)', () => {
  it('shows a loading indicator until the KPI fetch resolves', async () => {
    type AdminData = { totalUsers: number; activeIdeas: number; pendingEvaluations: number; health: string };
    let resolveFetch!: (value: AdminData) => void;
    const pending = new Promise<AdminData>((resolve) => {
      resolveFetch = resolve;
    });
    class DeferredStubApi {
      getAdmin() {
        return pending;
      }
    }

    await TestBed.configureTestingModule({
      imports: [AdminDashboardComponent],
      providers: [provideRouter([]), { provide: DashboardApiService, useClass: DeferredStubApi }],
    }).compileComponents();
    const fixture = TestBed.createComponent(AdminDashboardComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeTruthy();

    resolveFetch({ totalUsers: 12, activeIdeas: 30, pendingEvaluations: 5, health: 'Healthy' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeNull();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('12');
  });
});

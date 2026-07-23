import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CommitteeDashboardComponent } from './committee-dashboard.component';
import { DashboardApiService } from '../../core/dashboard-api.service';

class StubApi {
  getCommittee() {
    return Promise.resolve({ awaitingDecision: 4, decisionsThisWeek: 2 });
  }
}

describe('CommitteeDashboardComponent', () => {
  let fixture: ComponentFixture<CommitteeDashboardComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommitteeDashboardComponent],
      providers: [provideRouter([]), { provide: DashboardApiService, useClass: StubApi }],
    }).compileComponents();
    fixture = TestBed.createComponent(CommitteeDashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('renders committee KPIs and a queue CTA', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('4');
    expect(el.textContent).toContain('2');
    expect(Array.from(el.querySelectorAll('a')).some((a) => a.getAttribute('href')?.includes('/committee/queue'))).toBeTrue();
  });
});

describe('CommitteeDashboardComponent (loading state)', () => {
  it('shows a loading indicator until the KPI fetch resolves', async () => {
    let resolveFetch!: (value: { awaitingDecision: number; decisionsThisWeek: number }) => void;
    const pending = new Promise<{ awaitingDecision: number; decisionsThisWeek: number }>((resolve) => {
      resolveFetch = resolve;
    });
    class DeferredStubApi {
      getCommittee() {
        return pending;
      }
    }

    await TestBed.configureTestingModule({
      imports: [CommitteeDashboardComponent],
      providers: [provideRouter([]), { provide: DashboardApiService, useClass: DeferredStubApi }],
    }).compileComponents();
    const fixture = TestBed.createComponent(CommitteeDashboardComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeTruthy();

    resolveFetch({ awaitingDecision: 4, decisionsThisWeek: 2 });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeNull();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('4');
  });
});

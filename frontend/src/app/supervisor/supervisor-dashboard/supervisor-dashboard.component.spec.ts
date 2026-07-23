import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SupervisorApiService } from '../supervisor-api.service';
import { SupervisorDashboardComponent } from './supervisor-dashboard.component';

describe('SupervisorDashboardComponent', () => {
  let fixture: ComponentFixture<SupervisorDashboardComponent>;
  let supervisorApi: jasmine.SpyObj<SupervisorApiService>;

  function setup(): void {
    supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['previewFinalRanking', 'runFinalRanking']);

    TestBed.configureTestingModule({
      imports: [SupervisorDashboardComponent],
      providers: [provideRouter([]), { provide: SupervisorApiService, useValue: supervisorApi }],
    });
    fixture = TestBed.createComponent(SupervisorDashboardComponent);
  }

  it('renders links to the screening queue and track assignments', () => {
    setup();
    fixture.detectChanges();

    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.some((a) => a.getAttribute('href') === '/supervisor/screening')).toBe(true);
    expect(links.some((a) => a.getAttribute('href') === '/supervisor/track-assignments')).toBe(true);
  });

  it('renders the final ranking panel', () => {
    setup();
    fixture.detectChanges();

    const panel = fixture.nativeElement.querySelector('app-final-ranking-panel');
    expect(panel).toBeTruthy();
  });
});

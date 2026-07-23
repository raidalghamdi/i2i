import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ActivitiesComponent } from './activities.component';
import { PublicActivitiesApiService } from '../../core/public-activities-api.service';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { PublicActivity } from '../../core/public-data.model';

describe('ActivitiesComponent', () => {
  let fixture: ComponentFixture<ActivitiesComponent>;
  let activitiesApi: jasmine.SpyObj<PublicActivitiesApiService>;

  const ACTIVITY: PublicActivity = {
    id: 'a1',
    nameEn: 'Innovation Sprint',
    nameAr: 'سباق الابتكار',
    type: 'sprint',
    status: 'active',
    startDate: '2026-01-01',
    endDate: '2026-01-31',
    ideaCount: 12,
  };

  beforeEach(() => {
    activitiesApi = jasmine.createSpyObj('PublicActivitiesApiService', ['list']);
    activitiesApi.list.and.returnValue(Promise.resolve([ACTIVITY]));

    const contentApi = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    contentApi.getBySlug.and.returnValue(Promise.resolve(null));

    TestBed.configureTestingModule({
      imports: [ActivitiesComponent, HttpClientTestingModule],
      providers: [
        provideRouter([]),
        { provide: PublicActivitiesApiService, useValue: activitiesApi },
        { provide: PublicContentApiService, useValue: contentApi },
      ],
    });
    fixture = TestBed.createComponent(ActivitiesComponent);
  });

  it('renders a seeded activity name and links to its detail page', async () => {
    fixture.detectChanges();
    // Adaptation: zoneless app (see my-ideas-list.component.spec.ts) — await
    // ngOnInit() directly rather than relying on whenStable().
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Innovation Sprint');

    const link = fixture.nativeElement.querySelector('a[href="/activities/a1"]');
    expect(link).toBeTruthy();
  });

  it('shows an error state with retry when the list call fails, and recovers on retry', async () => {
    activitiesApi.list.and.returnValue(Promise.reject(new Error('boom')));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('a[href^="/activities/"]').length).toBe(0);
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    activitiesApi.list.and.returnValue(Promise.resolve([ACTIVITY]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Innovation Sprint');
  });

  it('shows an empty state when the list call succeeds with no activities', async () => {
    activitiesApi.list.and.returnValue(Promise.resolve([]));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No activities to show yet.');
  });
});

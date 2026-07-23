import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { ActivityDetailComponent } from './activity-detail.component';
import { PublicActivitiesApiService } from '../../core/public-activities-api.service';
import { PublicActivityDetail } from '../../core/public-data.model';

describe('ActivityDetailComponent', () => {
  let fixture: ComponentFixture<ActivityDetailComponent>;
  let api: jasmine.SpyObj<PublicActivitiesApiService>;

  const DETAIL: PublicActivityDetail = {
    activity: {
      id: 'a1',
      nameEn: 'Innovation Sprint',
      nameAr: 'سباق الابتكار',
      type: 'sprint',
      status: 'active',
      startDate: '2026-01-01',
      endDate: '2026-01-31',
      ideaCount: 12,
    },
    approvedCount: 5,
    pilotingCount: 2,
    ideas: [
      {
        id: 'i1',
        code: 'IDEA-0010',
        titleEn: 'Smart queue kiosk',
        titleAr: 'كشك طابور ذكي',
        status: 'submitted',
      },
    ],
  };

  function setup(id: string): void {
    api = jasmine.createSpyObj('PublicActivitiesApiService', ['getById']);

    TestBed.configureTestingModule({
      imports: [ActivityDetailComponent, HttpClientTestingModule],
      providers: [
        provideRouter([]),
        { provide: PublicActivitiesApiService, useValue: api },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => id } } } },
      ],
    });
    fixture = TestBed.createComponent(ActivityDetailComponent);
  }

  it('renders the activity name, KPIs, and a scoreboard idea', async () => {
    setup('a1');
    api.getById.and.returnValue(Promise.resolve(DETAIL));
    fixture.detectChanges();
    // Adaptation: zoneless app (see my-ideas-list.component.spec.ts) — await
    // ngOnInit() directly rather than relying on whenStable().
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Innovation Sprint');
    expect(text).toContain('12');
    expect(text).toContain('5');
    expect(text).toContain('2');
    expect(text).toContain('IDEA-0010');
    expect(text).toContain('Smart queue kiosk');

    const link = fixture.nativeElement.querySelector('a[href="/ideas/i1"]');
    expect(link).toBeTruthy();
  });

  it('shows a not-found message when the activity does not exist', async () => {
    setup('missing');
    api.getById.and.returnValue(Promise.resolve(null));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Activity not found');

    const link = fixture.nativeElement.querySelector('a[href="/activities"]');
    expect(link).toBeTruthy();
  });

  it('shows an error state with retry when the fetch fails, and recovers on retry', async () => {
    setup('a1');
    api.getById.and.returnValue(Promise.reject(new Error('boom')));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    api.getById.and.returnValue(Promise.resolve(DETAIL));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Innovation Sprint');
  });
});

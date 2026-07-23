import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { TrackDetailComponent } from './track-detail.component';
import { PublicTracksApiService } from '../../core/public-tracks-api.service';
import { PublicTrackDetail } from '../../core/public-data.model';

describe('TrackDetailComponent', () => {
  let fixture: ComponentFixture<TrackDetailComponent>;
  let api: jasmine.SpyObj<PublicTracksApiService>;

  const DETAIL: PublicTrackDetail = {
    track: {
      id: 't1',
      nameEn: 'Customer Experience',
      nameAr: 'تجربة العميل',
      descriptionEn: 'Ideas that improve how customers interact with services.',
      descriptionAr: 'أفكار تحسن تفاعل العملاء مع الخدمات.',
      priority: 1,
    },
    challenges: [
      {
        id: 'c1',
        textEn: 'Reduce average wait time by 20%.',
        textAr: 'تقليل متوسط وقت الانتظار بنسبة 20%.',
      },
    ],
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
    api = jasmine.createSpyObj('PublicTracksApiService', ['getById']);

    TestBed.configureTestingModule({
      imports: [TrackDetailComponent, HttpClientTestingModule],
      providers: [
        provideRouter([]),
        { provide: PublicTracksApiService, useValue: api },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => id } } } },
      ],
    });
    fixture = TestBed.createComponent(TrackDetailComponent);
  }

  it('renders the theme name, a challenge, and a related idea', async () => {
    setup('t1');
    api.getById.and.returnValue(Promise.resolve(DETAIL));
    fixture.detectChanges();
    // Adaptation: zoneless app (see my-ideas-list.component.spec.ts) — await
    // ngOnInit() directly rather than relying on whenStable().
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Customer Experience');
    expect(text).toContain('Reduce average wait time by 20%.');
    expect(text).toContain('IDEA-0010');
    expect(text).toContain('Smart queue kiosk');

    const link = fixture.nativeElement.querySelector('a[href="/ideas/i1"]');
    expect(link).toBeTruthy();
  });

  it('falls back to the default challenge list when none are seeded', async () => {
    setup('t1');
    api.getById.and.returnValue(
      Promise.resolve({ ...DETAIL, challenges: [], ideas: [] } as PublicTrackDetail),
    );
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain("Identify a real challenge within the track's scope");
  });

  it('shows a not-found message when the track does not exist', async () => {
    setup('missing');
    api.getById.and.returnValue(Promise.resolve(null));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Track not found');
    expect(text).toContain('Back to Tracks');
  });

  it('shows an error state with retry when the fetch fails, and recovers on retry', async () => {
    setup('t1');
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

    expect(fixture.nativeElement.textContent).toContain('Customer Experience');
  });
});

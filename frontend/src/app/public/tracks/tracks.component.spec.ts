import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TracksComponent } from './tracks.component';
import { PublicTracksApiService } from '../../core/public-tracks-api.service';
import { PublicContentApiService } from '../../core/public-content-api.service';
import { PublicTrack } from '../../core/public-data.model';

describe('TracksComponent', () => {
  let fixture: ComponentFixture<TracksComponent>;
  let tracksApi: jasmine.SpyObj<PublicTracksApiService>;

  const TRACK: PublicTrack = {
    id: 't1',
    nameEn: 'Customer Experience',
    nameAr: 'تجربة العميل',
    descriptionEn: 'Ideas that improve how customers interact with services.',
    descriptionAr: 'أفكار تحسن تفاعل العملاء مع الخدمات.',
    priority: 1,
  };

  beforeEach(() => {
    tracksApi = jasmine.createSpyObj('PublicTracksApiService', ['list']);
    tracksApi.list.and.returnValue(Promise.resolve([TRACK]));

    const contentApi = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    contentApi.getBySlug.and.returnValue(Promise.resolve(null));

    TestBed.configureTestingModule({
      imports: [TracksComponent, HttpClientTestingModule],
      providers: [
        provideRouter([]),
        { provide: PublicTracksApiService, useValue: tracksApi },
        { provide: PublicContentApiService, useValue: contentApi },
      ],
    });
    fixture = TestBed.createComponent(TracksComponent);
  });

  it('renders a seeded theme name and links to its detail page', async () => {
    fixture.detectChanges();
    // Adaptation: zoneless app (see my-ideas-list.component.spec.ts) — await
    // ngOnInit() directly rather than relying on whenStable().
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Customer Experience');

    const link = fixture.nativeElement.querySelector('a[href="/tracks/t1"]');
    expect(link).toBeTruthy();
  });

  it('shows an error state with retry when the list call fails, and recovers on retry', async () => {
    tracksApi.list.and.returnValue(Promise.reject(new Error('boom')));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('a[href^="/tracks/"]').length).toBe(0);
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    tracksApi.list.and.returnValue(Promise.resolve([TRACK]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Customer Experience');
  });

  it('shows an empty state when the list call succeeds with no tracks', async () => {
    tracksApi.list.and.returnValue(Promise.resolve([]));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No tracks to show yet.');
  });
});

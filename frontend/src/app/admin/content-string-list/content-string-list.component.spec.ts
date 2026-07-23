import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CmsApiService } from '../cms-api.service';
import { ContentString } from '../cms.model';
import { ContentStringListComponent } from './content-string-list.component';

describe('ContentStringListComponent', () => {
  let fixture: ComponentFixture<ContentStringListComponent>;
  let cmsApi: jasmine.SpyObj<CmsApiService>;

  function setup(strings: ContentString[]): void {
    cmsApi = jasmine.createSpyObj('CmsApiService', ['listStrings', 'deleteString']);
    cmsApi.listStrings.and.returnValue(Promise.resolve(strings));

    TestBed.configureTestingModule({
      imports: [ContentStringListComponent],
      providers: [provideRouter([]), { provide: CmsApiService, useValue: cmsApi }],
    });
    fixture = TestBed.createComponent(ContentStringListComponent);
  }

  it('renders one row per content string', async () => {
    setup([{ id: 's1', key: 'nav.home', valueAr: 'الرئيسية', valueEn: 'Home', updatedAt: '2026-01-01' }]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('nav.home');
  });

  it('shows an empty-state message when there are no strings', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No content strings');
  });

  it('deletes a content string and refreshes the list', async () => {
    setup([{ id: 's1', key: 'to-delete', valueAr: 'أ', valueEn: 'D', updatedAt: '2026-01-01' }]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    cmsApi.deleteString.and.returnValue(Promise.resolve());
    cmsApi.listStrings.and.returnValue(Promise.resolve([]));

    await fixture.componentInstance.onDelete('s1');

    expect(cmsApi.deleteString).toHaveBeenCalledWith('s1');
    expect(fixture.componentInstance.strings().length).toBe(0);
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    cmsApi = jasmine.createSpyObj('CmsApiService', ['listStrings', 'deleteString']);
    cmsApi.listStrings.and.returnValue(Promise.reject({ error: { error: 'Strings unavailable' } }));

    TestBed.configureTestingModule({
      imports: [ContentStringListComponent],
      providers: [provideRouter([]), { provide: CmsApiService, useValue: cmsApi }],
    });
    fixture = TestBed.createComponent(ContentStringListComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Strings unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    cmsApi.listStrings.and.returnValue(Promise.resolve([{ id: 's1', key: 'nav.recovered', valueAr: 'أ', valueEn: 'Recovered', updatedAt: '2026-01-01' }]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('nav.recovered');
  });
});

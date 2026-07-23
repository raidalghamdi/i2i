import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CmsApiService } from '../cms-api.service';
import { CmsContent } from '../cms.model';
import { CmsContentListComponent } from './cms-content-list.component';

describe('CmsContentListComponent', () => {
  let fixture: ComponentFixture<CmsContentListComponent>;
  let cmsApi: jasmine.SpyObj<CmsApiService>;

  function setup(content: CmsContent[]): void {
    cmsApi = jasmine.createSpyObj('CmsApiService', ['listContent', 'deleteContent']);
    cmsApi.listContent.and.returnValue(Promise.resolve(content));

    TestBed.configureTestingModule({
      imports: [CmsContentListComponent],
      providers: [provideRouter([]), { provide: CmsApiService, useValue: cmsApi }],
    });
    fixture = TestBed.createComponent(CmsContentListComponent);
  }

  it('renders one row per content page', async () => {
    setup([{ id: 'c1', slug: 'terms-and-conditions', titleAr: 'أ', titleEn: 'Terms', bodyAr: 'ب', bodyEn: 'Body', isPublished: true, publishedAt: null, updatedAt: '2026-01-01' }]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('terms-and-conditions');
  });

  it('shows an empty-state message when there is no content', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No page content');
  });

  it('deletes a content page and refreshes the list', async () => {
    setup([{ id: 'c1', slug: 'to-delete', titleAr: 'أ', titleEn: 'D', bodyAr: 'ب', bodyEn: 'B', isPublished: true, publishedAt: null, updatedAt: '2026-01-01' }]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    cmsApi.deleteContent.and.returnValue(Promise.resolve());
    cmsApi.listContent.and.returnValue(Promise.resolve([]));

    await fixture.componentInstance.onDelete('c1');

    expect(cmsApi.deleteContent).toHaveBeenCalledWith('c1');
    expect(fixture.componentInstance.content().length).toBe(0);
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    cmsApi = jasmine.createSpyObj('CmsApiService', ['listContent', 'deleteContent']);
    cmsApi.listContent.and.returnValue(Promise.reject({ error: { error: 'Content unavailable' } }));

    TestBed.configureTestingModule({
      imports: [CmsContentListComponent],
      providers: [provideRouter([]), { provide: CmsApiService, useValue: cmsApi }],
    });
    fixture = TestBed.createComponent(CmsContentListComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Content unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    cmsApi.listContent.and.returnValue(Promise.resolve([{ id: 'c1', slug: 'recovered', titleAr: 'أ', titleEn: 'Recovered', bodyAr: 'ب', bodyEn: 'B', isPublished: true, publishedAt: null, updatedAt: '2026-01-01' }]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('recovered');
  });
});

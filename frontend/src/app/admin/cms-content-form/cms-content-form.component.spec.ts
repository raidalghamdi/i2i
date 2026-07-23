import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { CmsApiService } from '../cms-api.service';
import { CmsContentFormComponent } from './cms-content-form.component';

describe('CmsContentFormComponent', () => {
  let fixture: ComponentFixture<CmsContentFormComponent>;
  let cmsApi: jasmine.SpyObj<CmsApiService>;
  let router: jasmine.SpyObj<Router>;

  const validFormValue = { slug: 'faq', titleAr: 'أ', titleEn: 'FAQ', bodyAr: 'ب', bodyEn: 'Body', isPublished: true };

  function setup(routeParamId: string | null): void {
    cmsApi = jasmine.createSpyObj('CmsApiService', ['getContent', 'createContent', 'updateContent']);
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [CmsContentFormComponent],
      providers: [
        { provide: CmsApiService, useValue: cmsApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => routeParamId } } } },
      ],
    });
    fixture = TestBed.createComponent(CmsContentFormComponent);
  }

  it('create mode: submits via createContent() then navigates to the list', async () => {
    setup(null);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    cmsApi.createContent.and.returnValue(Promise.resolve({ id: 'c1', ...validFormValue, publishedAt: null, updatedAt: '2026-01-01' }));
    fixture.componentInstance.form.setValue(validFormValue);

    await fixture.componentInstance.onSubmit();

    expect(cmsApi.createContent).toHaveBeenCalledWith(validFormValue);
    expect(router.navigate).toHaveBeenCalledWith(['/admin/cms/content']);
  });

  it('edit mode: pre-populates the form via getContent and submits via updateContent()', async () => {
    setup('c1');
    cmsApi.getContent.and.returnValue(Promise.resolve({ id: 'c1', slug: 'existing', titleAr: 'أ', titleEn: 'Existing', bodyAr: 'ب', bodyEn: 'Body', isPublished: false, publishedAt: null, updatedAt: '2026-01-01' }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.form.value.slug).toBe('existing');

    cmsApi.updateContent.and.returnValue(Promise.resolve({ id: 'c1', slug: 'existing', titleAr: 'أ', titleEn: 'Existing', bodyAr: 'ب', bodyEn: 'Body', isPublished: false, publishedAt: null, updatedAt: '2026-01-01' }));
    await fixture.componentInstance.onSubmit();

    expect(cmsApi.updateContent).toHaveBeenCalledWith('c1', jasmine.any(Object));
    expect(router.navigate).toHaveBeenCalledWith(['/admin/cms/content']);
  });

  it('marks the form invalid when required fields are empty', async () => {
    setup(null);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.form.invalid).toBe(true);
  });

  it('shows an inline error message when create fails', async () => {
    setup(null);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    cmsApi.createContent.and.returnValue(Promise.reject({ error: { error: 'A content page with this slug already exists.' } }));
    fixture.componentInstance.form.setValue(validFormValue);

    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('A content page with this slug already exists.');
  });

  it('shows an error state with retry when loading an existing page fails, and recovers on retry', async () => {
    setup('c1');
    cmsApi.getContent.and.returnValue(Promise.reject({ error: { error: 'Page unavailable' } }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Page unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    cmsApi.getContent.and.returnValue(Promise.resolve({ id: 'c1', slug: 'recovered', titleAr: 'أ', titleEn: 'Recovered', bodyAr: 'ب', bodyEn: 'Body', isPublished: true, publishedAt: null, updatedAt: '2026-01-01' }));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.componentInstance.form.value.slug).toBe('recovered');
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { CmsApiService } from '../cms-api.service';
import { CmsBlockFormComponent } from './cms-block-form.component';

describe('CmsBlockFormComponent', () => {
  let fixture: ComponentFixture<CmsBlockFormComponent>;
  let cmsApi: jasmine.SpyObj<CmsApiService>;
  let router: jasmine.SpyObj<Router>;

  function setup(routeParamId: string | null): void {
    cmsApi = jasmine.createSpyObj('CmsApiService', ['getBlock', 'createBlock', 'updateBlock']);
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [CmsBlockFormComponent],
      providers: [
        { provide: CmsApiService, useValue: cmsApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => routeParamId } } } },
      ],
    });
    fixture = TestBed.createComponent(CmsBlockFormComponent);
  }

  it('create mode: submits via createBlock() then navigates to the list', async () => {
    setup(null);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    cmsApi.createBlock.and.returnValue(Promise.resolve({ id: 'b1', key: 'k', contentAr: 'أ', contentEn: 'E', isPublished: true, updatedAt: '2026-01-01' }));
    fixture.componentInstance.form.setValue({ key: 'k', contentAr: 'أ', contentEn: 'E', isPublished: true });

    await fixture.componentInstance.onSubmit();

    expect(cmsApi.createBlock).toHaveBeenCalledWith({ key: 'k', contentAr: 'أ', contentEn: 'E', isPublished: true });
    expect(router.navigate).toHaveBeenCalledWith(['/admin/cms/blocks']);
  });

  it('edit mode: pre-populates the form via getBlock and submits via updateBlock()', async () => {
    setup('b1');
    cmsApi.getBlock.and.returnValue(Promise.resolve({ id: 'b1', key: 'existing', contentAr: 'أ', contentEn: 'Existing', isPublished: false, updatedAt: '2026-01-01' }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.form.value.key).toBe('existing');

    cmsApi.updateBlock.and.returnValue(Promise.resolve({ id: 'b1', key: 'existing', contentAr: 'أ', contentEn: 'Existing', isPublished: false, updatedAt: '2026-01-01' }));
    await fixture.componentInstance.onSubmit();

    expect(cmsApi.updateBlock).toHaveBeenCalledWith('b1', jasmine.any(Object));
    expect(router.navigate).toHaveBeenCalledWith(['/admin/cms/blocks']);
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

    cmsApi.createBlock.and.returnValue(Promise.reject({ error: { error: 'A block with this key already exists.' } }));
    fixture.componentInstance.form.setValue({ key: 'k', contentAr: 'أ', contentEn: 'E', isPublished: true });

    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('A block with this key already exists.');
  });

  it('shows an error state with retry when loading an existing block fails, and recovers on retry', async () => {
    setup('b1');
    cmsApi.getBlock.and.returnValue(Promise.reject({ error: { error: 'Block unavailable' } }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Block unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    cmsApi.getBlock.and.returnValue(Promise.resolve({ id: 'b1', key: 'recovered', contentAr: 'أ', contentEn: 'Recovered', isPublished: true, updatedAt: '2026-01-01' }));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.componentInstance.form.value.key).toBe('recovered');
  });
});

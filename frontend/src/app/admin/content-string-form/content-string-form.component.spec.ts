import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { CmsApiService } from '../cms-api.service';
import { ContentStringFormComponent } from './content-string-form.component';

describe('ContentStringFormComponent', () => {
  let fixture: ComponentFixture<ContentStringFormComponent>;
  let cmsApi: jasmine.SpyObj<CmsApiService>;
  let router: jasmine.SpyObj<Router>;

  const validFormValue = { key: 'nav.home', valueAr: 'الرئيسية', valueEn: 'Home' };

  function setup(routeParamId: string | null): void {
    cmsApi = jasmine.createSpyObj('CmsApiService', ['getString', 'createString', 'updateString']);
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [ContentStringFormComponent],
      providers: [
        { provide: CmsApiService, useValue: cmsApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => routeParamId } } } },
      ],
    });
    fixture = TestBed.createComponent(ContentStringFormComponent);
  }

  it('create mode: submits via createString() then navigates to the list', async () => {
    setup(null);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    cmsApi.createString.and.returnValue(Promise.resolve({ id: 's1', ...validFormValue, updatedAt: '2026-01-01' }));
    fixture.componentInstance.form.setValue(validFormValue);

    await fixture.componentInstance.onSubmit();

    expect(cmsApi.createString).toHaveBeenCalledWith(validFormValue);
    expect(router.navigate).toHaveBeenCalledWith(['/admin/cms/strings']);
  });

  it('edit mode: pre-populates the form via getString and submits via updateString()', async () => {
    setup('s1');
    cmsApi.getString.and.returnValue(Promise.resolve({ id: 's1', key: 'existing.key', valueAr: 'أ', valueEn: 'Existing', updatedAt: '2026-01-01' }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.form.value.key).toBe('existing.key');

    cmsApi.updateString.and.returnValue(Promise.resolve({ id: 's1', key: 'existing.key', valueAr: 'أ', valueEn: 'Existing', updatedAt: '2026-01-01' }));
    await fixture.componentInstance.onSubmit();

    expect(cmsApi.updateString).toHaveBeenCalledWith('s1', jasmine.any(Object));
    expect(router.navigate).toHaveBeenCalledWith(['/admin/cms/strings']);
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

    cmsApi.createString.and.returnValue(Promise.reject({ error: { error: 'A content string with this key already exists.' } }));
    fixture.componentInstance.form.setValue(validFormValue);

    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('A content string with this key already exists.');
  });

  it('shows an error state with retry when loading an existing string fails, and recovers on retry', async () => {
    setup('s1');
    cmsApi.getString.and.returnValue(Promise.reject({ error: { error: 'String unavailable' } }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('String unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    cmsApi.getString.and.returnValue(Promise.resolve({ id: 's1', key: 'recovered.key', valueAr: 'أ', valueEn: 'Recovered', updatedAt: '2026-01-01' }));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.componentInstance.form.value.key).toBe('recovered.key');
  });
});

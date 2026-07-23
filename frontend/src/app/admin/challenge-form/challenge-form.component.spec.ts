import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { ChallengeApiService } from '../challenge-api.service';
import { StrategicThemesService } from '../../ideas/strategic-themes.service';
import { ChallengeFormComponent } from './challenge-form.component';

describe('ChallengeFormComponent', () => {
  let fixture: ComponentFixture<ChallengeFormComponent>;
  let challengeApi: jasmine.SpyObj<ChallengeApiService>;
  let themesApi: jasmine.SpyObj<StrategicThemesService>;
  let router: jasmine.SpyObj<Router>;

  const themes = [{ id: 't1', nameAr: 'مسار', nameEn: 'Track One' }];
  const validFormValue = { strategicThemeId: 't1', textAr: 'أ', textEn: 'Challenge text', sortOrder: 0, isActive: true };

  function setup(routeParamId: string | null): void {
    challengeApi = jasmine.createSpyObj('ChallengeApiService', ['getById', 'create', 'update']);
    themesApi = jasmine.createSpyObj('StrategicThemesService', ['list']);
    themesApi.list.and.returnValue(Promise.resolve(themes));
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [ChallengeFormComponent],
      providers: [
        { provide: ChallengeApiService, useValue: challengeApi },
        { provide: StrategicThemesService, useValue: themesApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => routeParamId } } } },
      ],
    });
    fixture = TestBed.createComponent(ChallengeFormComponent);
  }

  it('create mode: submits via create() then navigates to the list', async () => {
    setup(null);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    challengeApi.create.and.returnValue(Promise.resolve({ id: 'c1', ...validFormValue }));
    fixture.componentInstance.form.setValue(validFormValue);

    await fixture.componentInstance.onSubmit();

    expect(challengeApi.create).toHaveBeenCalledWith(validFormValue);
    expect(router.navigate).toHaveBeenCalledWith(['/admin/challenges']);
  });

  it('edit mode: pre-populates the form via getById and submits via update()', async () => {
    setup('c1');
    challengeApi.getById.and.returnValue(Promise.resolve({ id: 'c1', strategicThemeId: 't1', textAr: 'موجود', textEn: 'Existing', sortOrder: 1, isActive: false }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.form.value.textEn).toBe('Existing');

    challengeApi.update.and.returnValue(Promise.resolve({ id: 'c1', strategicThemeId: 't1', textAr: 'موجود', textEn: 'Existing', sortOrder: 1, isActive: false }));
    await fixture.componentInstance.onSubmit();

    expect(challengeApi.update).toHaveBeenCalledWith('c1', jasmine.any(Object));
    expect(router.navigate).toHaveBeenCalledWith(['/admin/challenges']);
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

    challengeApi.create.and.returnValue(Promise.reject({ error: { error: 'Strategic theme does not exist.' } }));
    fixture.componentInstance.form.setValue(validFormValue);

    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('Strategic theme does not exist.');
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    challengeApi = jasmine.createSpyObj('ChallengeApiService', ['getById', 'create', 'update']);
    themesApi = jasmine.createSpyObj('StrategicThemesService', ['list']);
    themesApi.list.and.returnValue(Promise.reject({ error: { error: 'Tracks unavailable' } }));
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [ChallengeFormComponent],
      providers: [
        { provide: ChallengeApiService, useValue: challengeApi },
        { provide: StrategicThemesService, useValue: themesApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => null } } } },
      ],
    });
    fixture = TestBed.createComponent(ChallengeFormComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Tracks unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    themesApi.list.and.returnValue(Promise.resolve(themes));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.componentInstance.themes().length).toBe(1);
  });
});

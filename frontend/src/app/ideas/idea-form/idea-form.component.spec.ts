import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { IdeasApiService } from '../ideas-api.service';
import { StrategicThemesService } from '../strategic-themes.service';
import { IdeaFormComponent } from './idea-form.component';

describe('IdeaFormComponent', () => {
  let fixture: ComponentFixture<IdeaFormComponent>;
  let ideasApi: jasmine.SpyObj<IdeasApiService>;
  let themesApi: jasmine.SpyObj<StrategicThemesService>;
  let router: jasmine.SpyObj<Router>;

  const validFormValue = {
    titleAr: 'ا', titleEn: 'T', problemStatementAr: 'م', problemStatementEn: 'P',
    proposedSolutionAr: 'ح', proposedSolutionEn: 'S', expectedBenefitsAr: 'ف', expectedBenefitsEn: 'B',
    strategicThemeId: 'theme-1',
  };

  function setup(routeParamId: string | null): void {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['create', 'update', 'getById', 'uploadAttachment']);
    themesApi = jasmine.createSpyObj('StrategicThemesService', ['list']);
    router = jasmine.createSpyObj('Router', ['navigate']);
    themesApi.list.and.returnValue(Promise.resolve([{ id: 'theme-1', nameAr: 'أ', nameEn: 'Theme One' }]));

    TestBed.configureTestingModule({
      imports: [IdeaFormComponent],
      providers: [
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: StrategicThemesService, useValue: themesApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => routeParamId } } } },
      ],
    });
    fixture = TestBed.createComponent(IdeaFormComponent);
  }

  it('marks the form invalid when required fields are empty', async () => {
    setup(null);
    fixture.detectChanges();
    await fixture.whenStable();
    // Adaptation: this app is zoneless (no zone.js polyfill; see angular.json/main.ts),
    // so whenStable() does not reliably await the plain-Promise chain inside the async
    // ngOnInit (themesApi.list() then, in edit mode, ideasApi.getById()). Awaiting
    // ngOnInit() directly makes the wait deterministic; detectChanges() re-renders after.
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.form.invalid).toBe(true);
  });

  it('create mode: submits via create(), uploads queued files, then navigates', async () => {
    setup(null);
    fixture.detectChanges();
    await fixture.whenStable();
    // Adaptation: this app is zoneless (no zone.js polyfill; see angular.json/main.ts),
    // so whenStable() does not reliably await the plain-Promise chain inside the async
    // ngOnInit (themesApi.list() then, in edit mode, ideasApi.getById()). Awaiting
    // ngOnInit() directly makes the wait deterministic; detectChanges() re-renders after.
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    ideasApi.create.and.returnValue(Promise.resolve({ id: 'idea-1', code: 'IDEA-0001', status: 'draft' }));
    ideasApi.uploadAttachment.and.returnValue(Promise.resolve({ id: 'att-1', fileName: 'a.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }));

    fixture.componentInstance.form.setValue(validFormValue);
    const file = new File(['content'], 'a.pdf', { type: 'application/pdf' });
    fixture.componentInstance.queuedFiles.set([file]);

    await fixture.componentInstance.onSubmit();

    expect(ideasApi.create).toHaveBeenCalledWith(jasmine.objectContaining({ titleEn: 'T' }));
    expect(ideasApi.uploadAttachment).toHaveBeenCalledWith('idea-1', file);
    expect(router.navigate).toHaveBeenCalledWith(['/ideas', 'idea-1']);
  });

  it('create mode: after a successful create, retries as update() (not a duplicate create) if a later step fails', async () => {
    setup(null);
    fixture.detectChanges();
    await fixture.whenStable();
    // Adaptation: this app is zoneless (no zone.js polyfill; see angular.json/main.ts),
    // so whenStable() does not reliably await the plain-Promise chain inside the async
    // ngOnInit (themesApi.list() then, in edit mode, ideasApi.getById()). Awaiting
    // ngOnInit() directly makes the wait deterministic; detectChanges() re-renders after.
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    ideasApi.create.and.returnValue(Promise.resolve({ id: 'idea-1', code: 'IDEA-0001', status: 'draft' }));
    ideasApi.uploadAttachment.and.returnValue(Promise.reject({ error: { error: 'upload failed' } }));

    fixture.componentInstance.form.setValue(validFormValue);
    fixture.componentInstance.queuedFiles.set([new File(['content'], 'a.pdf', { type: 'application/pdf' })]);

    await fixture.componentInstance.onSubmit();
    expect(ideasApi.create).toHaveBeenCalledTimes(1);
    expect(fixture.componentInstance.errorMessage()).toBe('upload failed');

    ideasApi.uploadAttachment.and.returnValue(Promise.resolve({ id: 'att-1', fileName: 'a.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }));
    await fixture.componentInstance.onSubmit();

    expect(ideasApi.create).toHaveBeenCalledTimes(1);
    expect(ideasApi.update).toHaveBeenCalledWith('idea-1', jasmine.any(Object));
  });

  it('edit mode: pre-populates the form via getById and submits via update()', async () => {
    setup('idea-1');
    ideasApi.getById.and.returnValue(Promise.resolve({
      id: 'idea-1', code: 'IDEA-0001', submitterId: 'user-1', titleAr: 'ا', titleEn: 'Existing Title',
      problemStatementAr: 'م', problemStatementEn: 'P', proposedSolutionAr: 'ح', proposedSolutionEn: 'S',
      expectedBenefitsAr: 'ف', expectedBenefitsEn: 'B', strategicThemeId: 'theme-1',
      activityId: 'activity-1', challengeId: null, participationType: 'individual' as const, teamName: null, teamMembers: [],
      ipAcknowledged: true, termsAgreed: true,
      status: 'draft', currentStage: 0, updatedAt: '2026-01-01', attachments: [], screeningReason: null,
    }));
    fixture.detectChanges();
    await fixture.whenStable();
    // Adaptation: this app is zoneless (no zone.js polyfill; see angular.json/main.ts),
    // so whenStable() does not reliably await the plain-Promise chain inside the async
    // ngOnInit (themesApi.list() then, in edit mode, ideasApi.getById()). Awaiting
    // ngOnInit() directly makes the wait deterministic; detectChanges() re-renders after.
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.form.value.titleEn).toBe('Existing Title');

    ideasApi.update.and.returnValue(Promise.resolve({ id: 'idea-1', code: 'IDEA-0001' }));
    await fixture.componentInstance.onSubmit();

    expect(ideasApi.update).toHaveBeenCalledWith('idea-1', jasmine.any(Object));
    expect(router.navigate).toHaveBeenCalledWith(['/ideas', 'idea-1']);
  });

  it('shows an inline error message when create fails', async () => {
    setup(null);
    fixture.detectChanges();
    await fixture.whenStable();
    // Adaptation: this app is zoneless (no zone.js polyfill; see angular.json/main.ts),
    // so whenStable() does not reliably await the plain-Promise chain inside the async
    // ngOnInit (themesApi.list() then, in edit mode, ideasApi.getById()). Awaiting
    // ngOnInit() directly makes the wait deterministic; detectChanges() re-renders after.
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    ideasApi.create.and.returnValue(Promise.reject({ error: { error: 'Strategic theme does not exist.' } }));
    fixture.componentInstance.form.setValue(validFormValue);

    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('Strategic theme does not exist.');
  });

  it('renders the error state and retries the fetch when the "Try again" button is clicked', async () => {
    setup(null);
    themesApi.list.and.returnValue(Promise.reject(new Error('network error')));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).not.toBeNull();
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();

    themesApi.list.and.returnValue(Promise.resolve([{ id: 'theme-1', nameAr: 'أ', nameEn: 'Theme One' }]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.componentInstance.themes().length).toBe(1);
  });
});

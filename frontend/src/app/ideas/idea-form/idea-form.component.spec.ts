import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { IdeasApiService } from '../ideas-api.service';
import { StrategicThemesService } from '../strategic-themes.service';
import { ActivitiesService } from '../activities.service';
import { ChallengesService } from '../challenges.service';
import { DirectoryApiService } from '../../core/directory-api.service';
import { Idea } from '../idea.model';
import { IdeaFormComponent } from './idea-form.component';

describe('IdeaFormComponent', () => {
  let fixture: ComponentFixture<IdeaFormComponent>;
  let ideasApi: jasmine.SpyObj<IdeasApiService>;
  let themesApi: jasmine.SpyObj<StrategicThemesService>;
  let activitiesApi: jasmine.SpyObj<ActivitiesService>;
  let challengesApi: jasmine.SpyObj<ChallengesService>;
  let router: jasmine.SpyObj<Router>;

  function makeIdea(overrides: Partial<Idea> = {}): Idea {
    return {
      id: 'idea-1', code: 'IDEA-0001', submitterId: 'user-1', submitter: null,
      titleAr: 'ا', titleEn: 'ا', problemStatementAr: 'م', problemStatementEn: 'م',
      proposedSolutionAr: 'ح', proposedSolutionEn: 'ح', expectedBenefitsAr: 'ف', expectedBenefitsEn: 'ف',
      strategicThemeId: 'theme-1', activityId: 'activity-1', challengeId: null,
      participationType: 'individual', teamName: null, teamMembers: [],
      ipAcknowledged: true, termsAgreed: true,
      status: 'draft', currentStage: 0, createdAt: '2026-01-01', updatedAt: '2026-01-01',
      approvedAt: null, attachments: [], screeningReason: null, editableSections: null,
      ...overrides,
    };
  }

  function setup(routeParamId: string | null, idea?: Idea): void {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['update', 'resubmit', 'getById', 'uploadAttachment', 'deleteAttachment']); // Change 20260726
    themesApi = jasmine.createSpyObj('StrategicThemesService', ['list']);
    activitiesApi = jasmine.createSpyObj('ActivitiesService', ['list']);
    challengesApi = jasmine.createSpyObj('ChallengesService', ['listByTheme']);
    router = jasmine.createSpyObj('Router', ['navigate']);
    const directoryApi = jasmine.createSpyObj('DirectoryApiService', ['search']);
    directoryApi.search.and.returnValue(of([]));

    themesApi.list.and.returnValue(Promise.resolve([{ id: 'theme-1', nameAr: 'أ', nameEn: 'Theme One' }]));
    activitiesApi.list.and.returnValue(Promise.resolve([{ id: 'activity-1', nameAr: 'نشاط', nameEn: 'Activity' }]));
    challengesApi.listByTheme.and.returnValue(Promise.resolve([]));
    if (idea) ideasApi.getById.and.returnValue(Promise.resolve(idea));

    TestBed.configureTestingModule({
      imports: [IdeaFormComponent],
      providers: [
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: StrategicThemesService, useValue: themesApi },
        { provide: ActivitiesService, useValue: activitiesApi },
        { provide: ChallengesService, useValue: challengesApi },
        { provide: DirectoryApiService, useValue: directoryApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => routeParamId } } } },
      ],
    });
    fixture = TestBed.createComponent(IdeaFormComponent);
  }

  // This app is zoneless, so whenStable() doesn't await the plain-Promise chain in the async
  // ngOnInit; awaiting ngOnInit() directly makes the wait deterministic.
  async function boot(): Promise<void> {
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();
  }

  it('draft edit: pre-populates from getById and submits via update(), then uploads and navigates', async () => {
    setup('idea-1', makeIdea());
    await boot();

    expect(fixture.componentInstance.form.value.titleAr).toBe('ا');
    expect(fixture.componentInstance.isReturned()).toBe(false);

    ideasApi.update.and.returnValue(Promise.resolve({ id: 'idea-1', code: 'IDEA-0001' }));
    ideasApi.uploadAttachment.and.returnValue(Promise.resolve({ id: 'att-1', fileName: 'a.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }));
    const file = new File(['x'], 'a.pdf', { type: 'application/pdf' });
    fixture.componentInstance.queuedFiles.set([file]);

    await fixture.componentInstance.onSubmit();

    expect(ideasApi.update).toHaveBeenCalledWith('idea-1', jasmine.objectContaining({ titleAr: 'ا', participationType: 'individual' }));
    expect(ideasApi.resubmit).not.toHaveBeenCalled();
    expect(ideasApi.uploadAttachment).toHaveBeenCalledWith('idea-1', file);
    expect(router.navigate).toHaveBeenCalledWith(['/ideas', 'idea-1']);
  });

  it('returned with editableSections="title": only titleAr is enabled and the hint is shown', async () => {
    setup('idea-1', makeIdea({ status: 'returned', editableSections: 'title' }));
    await boot();

    expect(fixture.componentInstance.isReturned()).toBe(true);
    expect(fixture.componentInstance.form.get('titleAr')!.disabled).toBe(false);
    expect(fixture.componentInstance.form.get('proposedSolutionAr')!.disabled).toBe(true);
    expect(fixture.componentInstance.form.get('activityId')!.disabled).toBe(true);
    expect(fixture.componentInstance.form.get('participationType')!.disabled).toBe(true);
    expect(fixture.componentInstance.form.get('problemStatementAr')!.disabled).toBe(true);
    expect(fixture.componentInstance.sectionEditable('attachments')).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('returned for edits');
  });

  it('returned with "team,attachments": team and attachments are editable, others locked', async () => {
    setup('idea-1', makeIdea({ status: 'returned', editableSections: 'team,attachments', participationType: 'team', teamName: 'T' }));
    await boot();

    expect(fixture.componentInstance.sectionEditable('team')).toBe(true);
    expect(fixture.componentInstance.sectionEditable('attachments')).toBe(true);
    expect(fixture.componentInstance.sectionEditable('title')).toBe(false);
    expect(fixture.componentInstance.form.get('teamName')!.disabled).toBe(false);
    expect(fixture.componentInstance.form.get('titleAr')!.disabled).toBe(true);
  });

  it('returned with null editableSections: every section is editable', async () => {
    setup('idea-1', makeIdea({ status: 'returned', editableSections: null }));
    await boot();

    expect(fixture.componentInstance.isReturned()).toBe(true);
    for (const key of ['title', 'problem_statement', 'proposed_solution', 'expected_benefits', 'activity_id', 'strategic_theme_id', 'participation_type', 'team', 'attachments']) {
      expect(fixture.componentInstance.sectionEditable(key)).withContext(key).toBe(true);
    }
    // Problem statement and expected benefits are now individually unlockable, not force-locked.
    expect(fixture.componentInstance.form.get('problemStatementAr')!.disabled).toBe(false);
    expect(fixture.componentInstance.form.get('expectedBenefitsAr')!.disabled).toBe(false);
  });

  it('returned edit submits via resubmit() (not update) and navigates', async () => {
    setup('idea-1', makeIdea({ status: 'returned', editableSections: 'title' }));
    await boot();

    ideasApi.resubmit.and.returnValue(Promise.resolve({ id: 'idea-1', status: 'submitted' }));
    fixture.componentInstance.form.get('titleAr')!.setValue('new title');

    await fixture.componentInstance.onSubmit();

    expect(ideasApi.resubmit).toHaveBeenCalledWith('idea-1', jasmine.objectContaining({ titleAr: 'new title' }));
    expect(ideasApi.update).not.toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/ideas', 'idea-1']);
  });

  it('returned edit uploads queued attachments before resubmitting when attachments are editable', async () => {
    setup('idea-1', makeIdea({ status: 'returned', editableSections: 'attachments' }));
    await boot();

    const calls: string[] = [];
    ideasApi.uploadAttachment.and.callFake(() => { calls.push('upload'); return Promise.resolve({ id: 'att-1', fileName: 'a.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }); });
    ideasApi.resubmit.and.callFake(() => { calls.push('resubmit'); return Promise.resolve({ id: 'idea-1', status: 'submitted' }); });
    fixture.componentInstance.queuedFiles.set([new File(['x'], 'a.pdf', { type: 'application/pdf' })]);

    await fixture.componentInstance.onSubmit();

    expect(calls).toEqual(['upload', 'resubmit']);
  });

  it('surfaces an inline error message when the save fails', async () => {
    setup('idea-1', makeIdea());
    await boot();

    ideasApi.update.and.returnValue(Promise.reject({ error: { error: 'Strategic theme does not exist.' } }));
    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('Strategic theme does not exist.');
  });

  it('renders the error state and retries the fetch when "Try again" is clicked', async () => {
    setup('idea-1', makeIdea());
    themesApi.list.and.returnValue(Promise.reject(new Error('network error')));
    await boot();

    expect(fixture.componentInstance.loadError()).not.toBeNull();
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();

    themesApi.list.and.returnValue(Promise.resolve([{ id: 'theme-1', nameAr: 'أ', nameEn: 'Theme One' }]));
    await fixture.componentInstance.reload();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
  });

  const attachment = { id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 12, uploadedAt: '2026-01-01' }; // Change 20260726

  it('deletes an existing attachment after confirmation and drops it from the list', async () => { // Change 20260726
    setup('idea-1', makeIdea({ attachments: [attachment] })); // Change 20260726
    await boot(); // Change 20260726
    spyOn(window, 'confirm').and.returnValue(true); // Change 20260726
    ideasApi.deleteAttachment.and.returnValue(Promise.resolve()); // Change 20260726

    const button = fixture.nativeElement.querySelector('[data-testid="delete-attachment"]') as HTMLButtonElement; // Change 20260726
    expect(button).toBeTruthy(); // Change 20260726
    button.click(); // Change 20260726
    await fixture.whenStable(); // Change 20260726
    fixture.detectChanges(); // Change 20260726

    expect(ideasApi.deleteAttachment).toHaveBeenCalledWith('idea-1', 'att-1'); // Change 20260726
    expect(fixture.componentInstance.existingAttachments()).toEqual([]); // Change 20260726
    expect(fixture.nativeElement.textContent).not.toContain('evidence.pdf'); // Change 20260726
    expect(fixture.componentInstance.deletingAttachmentId()).toBeNull(); // Change 20260726
  }); // Change 20260726

  it('does not delete when the confirmation is dismissed', async () => { // Change 20260726
    setup('idea-1', makeIdea({ attachments: [attachment] })); // Change 20260726
    await boot(); // Change 20260726
    spyOn(window, 'confirm').and.returnValue(false); // Change 20260726

    (fixture.nativeElement.querySelector('[data-testid="delete-attachment"]') as HTMLButtonElement).click(); // Change 20260726
    await fixture.whenStable(); // Change 20260726

    expect(ideasApi.deleteAttachment).not.toHaveBeenCalled(); // Change 20260726
    expect(fixture.componentInstance.existingAttachments().length).toBe(1); // Change 20260726
  }); // Change 20260726

  it('shows an error banner and keeps the attachment when deletion fails', async () => { // Change 20260726
    setup('idea-1', makeIdea({ attachments: [attachment] })); // Change 20260726
    await boot(); // Change 20260726
    spyOn(window, 'confirm').and.returnValue(true); // Change 20260726
    ideasApi.deleteAttachment.and.returnValue(Promise.reject({ error: { error: 'Attachment is locked.' } })); // Change 20260726

    (fixture.nativeElement.querySelector('[data-testid="delete-attachment"]') as HTMLButtonElement).click(); // Change 20260726
    await fixture.whenStable(); // Change 20260726
    fixture.detectChanges(); // Change 20260726

    const banner = fixture.nativeElement.querySelector('[data-testid="attachment-error"]'); // Change 20260726
    expect(banner?.textContent).toContain('Attachment is locked.'); // Change 20260726
    expect(fixture.componentInstance.existingAttachments().length).toBe(1); // Change 20260726
    expect(fixture.componentInstance.deletingAttachmentId()).toBeNull(); // Change 20260726
  }); // Change 20260726

  it('hides the delete control when a return locked the attachments section', async () => { // Change 20260726
    setup('idea-1', makeIdea({ status: 'returned', editableSections: 'title', attachments: [attachment] })); // Change 20260726
    await boot(); // Change 20260726

    expect(fixture.nativeElement.querySelector('[data-testid="delete-attachment"]')).toBeNull(); // Change 20260726
  }); // Change 20260726
});

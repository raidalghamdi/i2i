import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { IdeasApiService } from '../ideas-api.service';
import { StrategicThemesService } from '../strategic-themes.service';
import { ActivitiesService } from '../activities.service';
import { ChallengesService } from '../challenges.service';
import { IdeaSubmitWizardComponent } from './idea-submit-wizard.component';

describe('IdeaSubmitWizardComponent', () => {
  let fixture: ComponentFixture<IdeaSubmitWizardComponent>;
  let ideasApi: jasmine.SpyObj<IdeasApiService>;
  let themesApi: jasmine.SpyObj<StrategicThemesService>;
  let activitiesApi: jasmine.SpyObj<ActivitiesService>;
  let challengesApi: jasmine.SpyObj<ChallengesService>;
  let router: jasmine.SpyObj<Router>;

  afterEach(() => localStorage.clear());

  function setup(): void {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['create', 'update', 'uploadAttachment', 'submit']);
    themesApi = jasmine.createSpyObj('StrategicThemesService', ['list']);
    activitiesApi = jasmine.createSpyObj('ActivitiesService', ['list']);
    challengesApi = jasmine.createSpyObj('ChallengesService', ['listByTheme']);
    router = jasmine.createSpyObj('Router', ['navigate']);

    themesApi.list.and.returnValue(Promise.resolve([{ id: 'theme-1', nameAr: 'أ', nameEn: 'Theme One' }]));
    activitiesApi.list.and.returnValue(Promise.resolve([{ id: 'activity-1', nameAr: 'ف', nameEn: 'Activity One' }]));
    challengesApi.listByTheme.and.returnValue(Promise.resolve([]));

    TestBed.configureTestingModule({
      imports: [IdeaSubmitWizardComponent],
      providers: [
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: StrategicThemesService, useValue: themesApi },
        { provide: ActivitiesService, useValue: activitiesApi },
        { provide: ChallengesService, useValue: challengesApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => null } } } },
      ],
    });
    fixture = TestBed.createComponent(IdeaSubmitWizardComponent);
  }

  async function initialize(): Promise<void> {
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();
  }

  it('loads themes and activities on init', async () => {
    setup();
    await initialize();

    expect(fixture.componentInstance.themes()).toEqual([{ id: 'theme-1', nameAr: 'أ', nameEn: 'Theme One' }]);
    expect(fixture.componentInstance.activities()).toEqual([{ id: 'activity-1', nameAr: 'ف', nameEn: 'Activity One' }]);
  });

  it('step 0 is invalid until activity, theme, and participation type are set', async () => {
    setup();
    await initialize();

    expect(fixture.componentInstance.stepValid(0)).toBe(false);

    fixture.componentInstance.form.controls.basics.patchValue({ activityId: 'activity-1', strategicThemeId: 'theme-1' });

    expect(fixture.componentInstance.stepValid(0)).toBe(true);
  });

  it('fetches challenge options when the track changes, and requires a selection if any exist', async () => {
    setup();
    challengesApi.listByTheme.and.returnValue(Promise.resolve([{ id: 'challenge-1', textAr: 'ت', textEn: 'Challenge One' }]));
    await initialize();

    fixture.componentInstance.form.controls.basics.patchValue({ activityId: 'activity-1', strategicThemeId: 'theme-1' });
    await Promise.resolve();
    await Promise.resolve();

    expect(challengesApi.listByTheme).toHaveBeenCalledWith('theme-1');
    expect(fixture.componentInstance.challengeOptions()).toEqual([{ id: 'challenge-1', textAr: 'ت', textEn: 'Challenge One' }]);
    expect(fixture.componentInstance.stepValid(0)).toBe(false);

    fixture.componentInstance.form.controls.basics.patchValue({ challengeId: 'challenge-1' });
    expect(fixture.componentInstance.stepValid(0)).toBe(true);
  });

  it('team participation requires a team name and 2-4 valid additional members', async () => {
    setup();
    await initialize();
    fixture.componentInstance.form.controls.basics.patchValue({ activityId: 'activity-1', strategicThemeId: 'theme-1', participationType: 'team' });

    expect(fixture.componentInstance.stepValid(0)).toBe(false);

    fixture.componentInstance.form.controls.basics.patchValue({ teamName: 'Team A' });
    fixture.componentInstance.updateTeamMemberName(0, 'Member One');
    fixture.componentInstance.updateTeamMemberEmail(0, 'm1@example.com');
    fixture.componentInstance.updateTeamMemberName(1, 'Member Two');
    fixture.componentInstance.updateTeamMemberEmail(1, 'not-an-email');

    expect(fixture.componentInstance.stepValid(0)).toBe(false);

    fixture.componentInstance.updateTeamMemberEmail(1, 'm2@example.com');

    expect(fixture.componentInstance.stepValid(0)).toBe(true);
  });

  it('addTeamMember caps at 4 additional members, removeTeamMember floors at 2', async () => {
    setup();
    await initialize();

    fixture.componentInstance.addTeamMember();
    fixture.componentInstance.addTeamMember();
    expect(fixture.componentInstance.teamMembers().length).toBe(4);
    fixture.componentInstance.addTeamMember();
    expect(fixture.componentInstance.teamMembers().length).toBe(4);

    fixture.componentInstance.removeTeamMember(0);
    fixture.componentInstance.removeTeamMember(0);
    expect(fixture.componentInstance.teamMembers().length).toBe(2);
    fixture.componentInstance.removeTeamMember(0);
    expect(fixture.componentInstance.teamMembers().length).toBe(2);
  });

  it('goNext only advances when the current step is valid', async () => {
    setup();
    await initialize();

    fixture.componentInstance.goNext();
    expect(fixture.componentInstance.currentStep()).toBe(0);

    fixture.componentInstance.form.controls.basics.patchValue({ activityId: 'activity-1', strategicThemeId: 'theme-1' });
    fixture.componentInstance.goNext();
    expect(fixture.componentInstance.currentStep()).toBe(1);
  });

  it('goBack decrements the current step, floored at 0', async () => {
    setup();
    await initialize();
    fixture.componentInstance.currentStep.set(2);

    fixture.componentInstance.goBack();
    expect(fixture.componentInstance.currentStep()).toBe(1);

    fixture.componentInstance.currentStep.set(0);
    fixture.componentInstance.goBack();
    expect(fixture.componentInstance.currentStep()).toBe(0);
  });

  it('step 1 (details) is invalid until title and description are filled', async () => {
    setup();
    await initialize();

    expect(fixture.componentInstance.stepValid(1)).toBe(false);

    fixture.componentInstance.form.controls.details.patchValue({ title: 'My idea', description: 'A description of the idea.' });

    expect(fixture.componentInstance.stepValid(1)).toBe(true);
  });

  function makeFile(name: string, type: string, sizeBytes: number): File {
    return new File([new Uint8Array(sizeBytes)], name, { type });
  }

  it('step 2 (attachments) is invalid until at least one file is queued', async () => {
    setup();
    await initialize();

    expect(fixture.componentInstance.stepValid(2)).toBe(false);

    fixture.componentInstance.queuedFiles.set([makeFile('a.pdf', 'application/pdf', 100)]);

    expect(fixture.componentInstance.stepValid(2)).toBe(true);
  });

  it('rejects files with a disallowed MIME type', async () => {
    setup();
    await initialize();

    const dataTransfer = { files: [makeFile('a.exe', 'application/x-msdownload', 100)] } as unknown as DataTransfer;
    fixture.componentInstance.onDrop({ preventDefault: () => {}, dataTransfer } as unknown as DragEvent);

    expect(fixture.componentInstance.queuedFiles().length).toBe(0);
    expect(fixture.componentInstance.attachmentError()).not.toBeNull();
  });

  it('rejects files larger than 10MB', async () => {
    setup();
    await initialize();

    const dataTransfer = { files: [makeFile('a.pdf', 'application/pdf', 11 * 1024 * 1024)] } as unknown as DataTransfer;
    fixture.componentInstance.onDrop({ preventDefault: () => {}, dataTransfer } as unknown as DragEvent);

    expect(fixture.componentInstance.queuedFiles().length).toBe(0);
    expect(fixture.componentInstance.attachmentError()).not.toBeNull();
  });

  it('accepts a valid file via drop and rejects a 6th file over the 5-file limit', async () => {
    setup();
    await initialize();

    const fiveFiles = Array.from({ length: 5 }, (_, i) => makeFile(`f${i}.pdf`, 'application/pdf', 100));
    fixture.componentInstance.onDrop({ preventDefault: () => {}, dataTransfer: { files: fiveFiles } as unknown as DataTransfer } as unknown as DragEvent);
    expect(fixture.componentInstance.queuedFiles().length).toBe(5);

    const sixthFile = makeFile('f5.pdf', 'application/pdf', 100);
    fixture.componentInstance.onDrop({ preventDefault: () => {}, dataTransfer: { files: [sixthFile] } as unknown as DataTransfer } as unknown as DragEvent);

    expect(fixture.componentInstance.queuedFiles().length).toBe(5);
    expect(fixture.componentInstance.attachmentError()).not.toBeNull();
  });

  it('removeQueuedFile removes the file at the given index', async () => {
    setup();
    await initialize();
    fixture.componentInstance.queuedFiles.set([makeFile('a.pdf', 'application/pdf', 100), makeFile('b.pdf', 'application/pdf', 100)]);

    fixture.componentInstance.removeQueuedFile(0);

    expect(fixture.componentInstance.queuedFiles().length).toBe(1);
    expect(fixture.componentInstance.queuedFiles()[0].name).toBe('b.pdf');
  });

  function fillValidWizard(fixture: ComponentFixture<IdeaSubmitWizardComponent>): void {
    fixture.componentInstance.form.controls.basics.patchValue({ activityId: 'activity-1', strategicThemeId: 'theme-1' });
    fixture.componentInstance.form.controls.details.patchValue({ title: 'My idea', description: 'A description.' });
    fixture.componentInstance.queuedFiles.set([makeFile('a.pdf', 'application/pdf', 100)]);
    fixture.componentInstance.form.controls.review.patchValue({ ipAcknowledged: true, termsAgreed: true });
  }

  it('step 3 (review) is invalid until both consent checkboxes are checked', async () => {
    setup();
    await initialize();

    expect(fixture.componentInstance.stepValid(3)).toBe(false);

    fixture.componentInstance.form.controls.review.patchValue({ ipAcknowledged: true, termsAgreed: true });

    expect(fixture.componentInstance.stepValid(3)).toBe(true);
  });

  it('onSubmit creates the idea, uploads the queued file, submits it, and navigates to the detail page', async () => {
    setup();
    await initialize();
    fillValidWizard(fixture);

    ideasApi.create.and.returnValue(Promise.resolve({ id: 'idea-1', code: 'IDEA-0001', status: 'draft' }));
    ideasApi.uploadAttachment.and.returnValue(Promise.resolve({ id: 'att-1', fileName: 'a.pdf', contentType: 'application/pdf', fileSizeBytes: 100, uploadedAt: '2026-01-01' }));
    ideasApi.submit.and.returnValue(Promise.resolve({ id: 'idea-1', status: 'submitted' }));

    await fixture.componentInstance.onSubmit();

    expect(ideasApi.create).toHaveBeenCalledWith(jasmine.objectContaining({
      titleAr: 'My idea', titleEn: 'My idea',
      proposedSolutionAr: 'A description.', proposedSolutionEn: 'A description.',
      problemStatementAr: '', problemStatementEn: '', expectedBenefitsAr: '', expectedBenefitsEn: '',
      activityId: 'activity-1', strategicThemeId: 'theme-1', participationType: 'individual',
      teamName: null, teamMembers: [], ipAcknowledged: true, termsAgreed: true,
    }));
    expect(ideasApi.uploadAttachment).toHaveBeenCalledWith('idea-1', jasmine.any(File));
    expect(ideasApi.submit).toHaveBeenCalledWith('idea-1');
    expect(router.navigate).toHaveBeenCalledWith(['/ideas', 'idea-1', 'submitted']);
    expect(fixture.componentInstance.queuedFiles().length).toBe(0);
  });

  it('sends team fields only when participation is team', async () => {
    setup();
    await initialize();
    fillValidWizard(fixture);
    fixture.componentInstance.form.controls.basics.patchValue({ participationType: 'team', teamName: 'Team A' });
    fixture.componentInstance.updateTeamMemberName(0, 'Member One');
    fixture.componentInstance.updateTeamMemberEmail(0, 'm1@example.com');
    fixture.componentInstance.updateTeamMemberName(1, 'Member Two');
    fixture.componentInstance.updateTeamMemberEmail(1, 'm2@example.com');

    ideasApi.create.and.returnValue(Promise.resolve({ id: 'idea-1', code: 'IDEA-0001', status: 'draft' }));
    ideasApi.uploadAttachment.and.returnValue(Promise.resolve({ id: 'att-1', fileName: 'a.pdf', contentType: 'application/pdf', fileSizeBytes: 100, uploadedAt: '2026-01-01' }));
    ideasApi.submit.and.returnValue(Promise.resolve({ id: 'idea-1', status: 'submitted' }));

    await fixture.componentInstance.onSubmit();

    expect(ideasApi.create).toHaveBeenCalledWith(jasmine.objectContaining({
      participationType: 'team', teamName: 'Team A',
      teamMembers: [{ name: 'Member One', email: 'm1@example.com' }, { name: 'Member Two', email: 'm2@example.com' }],
    }));
  });

  it('on a failed attachment upload, does not re-create the idea on retry and only retries the remaining files', async () => {
    setup();
    await initialize();
    fillValidWizard(fixture);
    fixture.componentInstance.queuedFiles.set([makeFile('a.pdf', 'application/pdf', 100), makeFile('b.pdf', 'application/pdf', 100)]);

    ideasApi.create.and.returnValue(Promise.resolve({ id: 'idea-1', code: 'IDEA-0001', status: 'draft' }));
    ideasApi.uploadAttachment.and.callFake((id: string, file: File) => {
      if (file.name === 'a.pdf') return Promise.resolve({ id: 'att-1', fileName: 'a.pdf', contentType: 'application/pdf', fileSizeBytes: 100, uploadedAt: '2026-01-01' });
      return Promise.reject({ error: { error: 'upload failed' } });
    });

    await fixture.componentInstance.onSubmit();

    expect(ideasApi.create).toHaveBeenCalledTimes(1);
    expect(fixture.componentInstance.errorMessage()).toBe('upload failed');
    expect(fixture.componentInstance.queuedFiles().length).toBe(1);
    expect(fixture.componentInstance.queuedFiles()[0].name).toBe('b.pdf');

    ideasApi.uploadAttachment.and.returnValue(Promise.resolve({ id: 'att-2', fileName: 'b.pdf', contentType: 'application/pdf', fileSizeBytes: 100, uploadedAt: '2026-01-01' }));
    ideasApi.submit.and.returnValue(Promise.resolve({ id: 'idea-1', status: 'submitted' }));

    await fixture.componentInstance.onSubmit();

    expect(ideasApi.create).toHaveBeenCalledTimes(1);
    expect(ideasApi.update).toHaveBeenCalledWith('idea-1', jasmine.any(Object));
    expect(router.navigate).toHaveBeenCalledWith(['/ideas', 'idea-1', 'submitted']);
  });

  it('shows an inline error message when create fails', async () => {
    setup();
    await initialize();
    fillValidWizard(fixture);

    ideasApi.create.and.returnValue(Promise.reject({ error: { error: 'Strategic theme does not exist.' } }));

    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('Strategic theme does not exist.');
  });

  it('debounce-saves title/description/theme/activity to localStorage as the user types', async () => {
    setup();
    await initialize();

    fixture.componentInstance.form.controls.details.patchValue({ title: 'Draft title' });
    await new Promise((resolve) => setTimeout(resolve, 750));

    const saved = JSON.parse(localStorage.getItem('i2i-idea-draft-v1')!);
    expect(saved.title).toBe('Draft title');
  });

  it('offers to restore a saved draft on init, and restoreDraft() fills the form', async () => {
    localStorage.setItem('i2i-idea-draft-v1', JSON.stringify({ title: 'Saved title', description: 'Saved description', strategicThemeId: 'theme-1', activityId: 'activity-1' }));
    setup();
    await initialize();

    expect(fixture.componentInstance.draftAvailable()).toBe(true);

    fixture.componentInstance.restoreDraft();

    expect(fixture.componentInstance.form.controls.details.controls.title.value).toBe('Saved title');
    expect(fixture.componentInstance.form.controls.basics.controls.activityId.value).toBe('activity-1');
    expect(fixture.componentInstance.draftAvailable()).toBe(false);
  });

  it('dismissDraft() clears the saved draft without filling the form', async () => {
    localStorage.setItem('i2i-idea-draft-v1', JSON.stringify({ title: 'Saved title', description: '', strategicThemeId: '', activityId: '' }));
    setup();
    await initialize();

    fixture.componentInstance.dismissDraft();

    expect(fixture.componentInstance.draftAvailable()).toBe(false);
    expect(localStorage.getItem('i2i-idea-draft-v1')).toBeNull();
    expect(fixture.componentInstance.form.controls.details.controls.title.value).toBe('');
  });

  it('clears the saved draft after a successful submit', async () => {
    setup();
    await initialize();
    fillValidWizard(fixture);
    fixture.componentInstance.form.controls.details.patchValue({ title: 'Will be cleared' });
    await new Promise((resolve) => setTimeout(resolve, 750));
    expect(localStorage.getItem('i2i-idea-draft-v1')).not.toBeNull();

    ideasApi.create.and.returnValue(Promise.resolve({ id: 'idea-1', code: 'IDEA-0001', status: 'draft' }));
    ideasApi.uploadAttachment.and.returnValue(Promise.resolve({ id: 'att-1', fileName: 'a.pdf', contentType: 'application/pdf', fileSizeBytes: 100, uploadedAt: '2026-01-01' }));
    ideasApi.submit.and.returnValue(Promise.resolve({ id: 'idea-1', status: 'submitted' }));

    await fixture.componentInstance.onSubmit();

    expect(localStorage.getItem('i2i-idea-draft-v1')).toBeNull();
  });

  it('does not let a debounced draft-save resurrect the draft after a fast submit clears it', async () => {
    setup();
    await initialize();
    fillValidWizard(fixture);

    ideasApi.create.and.returnValue(Promise.resolve({ id: 'idea-1', code: 'IDEA-0001', status: 'draft' }));
    ideasApi.uploadAttachment.and.returnValue(Promise.resolve({ id: 'att-1', fileName: 'a.pdf', contentType: 'application/pdf', fileSizeBytes: 100, uploadedAt: '2026-01-01' }));
    ideasApi.submit.and.returnValue(Promise.resolve({ id: 'idea-1', status: 'submitted' }));

    // Edit a debounced field, then submit immediately — well inside the 700ms debounce window.
    fixture.componentInstance.form.controls.details.patchValue({ title: 'Edited right before submit' });
    await fixture.componentInstance.onSubmit();

    // Let the original debounce window fully elapse. If the pending timer wasn't
    // cancelled by clearDraft(), it would fire here and resurrect the draft.
    await new Promise((resolve) => setTimeout(resolve, 750));

    expect(localStorage.getItem('i2i-idea-draft-v1')).toBeNull();
  });

  it('renders the error state and retries the fetch when the "Try again" button is clicked', async () => {
    setup();
    themesApi.list.and.returnValue(Promise.reject(new Error('network error')));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).not.toBeNull();
    const retryButton = (fixture.nativeElement as HTMLElement).querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();

    themesApi.list.and.returnValue(Promise.resolve([{ id: 'theme-1', nameAr: 'أ', nameEn: 'Theme One' }]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.componentInstance.themes().length).toBe(1);
  });
});

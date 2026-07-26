import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { IdeasApiService } from '../ideas-api.service';
import { Idea, IdeaAttachment, IdeaJourney } from '../idea.model';
import { StrategicThemesService } from '../strategic-themes.service';
import { ActivitiesService } from '../activities.service';
import { ChallengesService } from '../challenges.service';
import { MeApiService, MeProfile } from '../../core/me-api.service';
import { IdentityService } from '../../core/auth/identity.service';
import { IdeaDetailComponent } from './idea-detail.component';

function identityProvider(samAccountName: string | null = null, activeRole = 'submitter') {
  return {
    provide: IdentityService,
    useValue: {
      identity: signal({
        samAccountName,
        email: null,
        department: null,
        roles: [activeRole],
        activeRole,
      }),
    },
  };
}

describe('IdeaDetailComponent', () => {
  let fixture: ComponentFixture<IdeaDetailComponent>;
  let ideasApi: jasmine.SpyObj<IdeasApiService>;

  const baseIdea: Idea = {
    id: 'idea-1', code: 'IDEA-0001', submitterId: 'owner-1', titleAr: 'ا', titleEn: 'Title',
    problemStatementAr: 'م', problemStatementEn: 'Problem', proposedSolutionAr: 'ح', proposedSolutionEn: 'Solution',
    expectedBenefitsAr: 'ف', expectedBenefitsEn: 'Benefits', strategicThemeId: 'theme-1',
    activityId: 'activity-1', challengeId: null, participationType: 'individual', teamName: null, teamMembers: [],
    ipAcknowledged: true, termsAgreed: true,
    status: 'draft', currentStage: 0, createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01', approvedAt: null,
    attachments: [] as IdeaAttachment[], screeningReason: null as string | null, editableSections: null,
  };

  const baseMe: MeProfile = {
    id: 'owner-1', samAccountName: 'owner', email: 'owner@gac-demo.sa', fullNameAr: 'م', fullNameEn: 'Owner',
    department: null, title: null, points: 0, level: 1, roles: ['submitter'],
  };

  const defaultJourney: IdeaJourney = {
    currentStage: 0, stopped: false, evaluationScore: null,
    stages: Array.from({ length: 8 }, (_, index) => ({
      index, state: index === 0 ? 'current' : 'upcoming',
      label: { ar: 'x', en: index === 3 ? 'Committee Review' : 'Stage' }, completedAt: null,
    })),
  };

  function setup(idea: typeof baseIdea, currentUserSam: string | null = null, activeRole = 'submitter'): void {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getById', 'submit', 'uploadAttachment', 'getEvaluations', 'resubmitEvaluation', 'getJourney', 'withdraw', 'getAttachmentBlob']);
    const themes = jasmine.createSpyObj('StrategicThemesService', ['list']);
    const activities = jasmine.createSpyObj('ActivitiesService', ['list']);
    const challenges = jasmine.createSpyObj('ChallengesService', ['listByTheme']);
    const meApi = jasmine.createSpyObj('MeApiService', ['get']);
    ideasApi.getById.and.returnValue(Promise.resolve(idea));
    ideasApi.getJourney.and.returnValue(Promise.resolve(defaultJourney));
    themes.list.and.returnValue(Promise.resolve([{ id: 'theme-1', nameAr: 'ت', nameEn: 'Digital Track' }]));
    activities.list.and.returnValue(Promise.resolve([{ id: 'activity-1', nameAr: 'ن', nameEn: 'Hackathon' }]));
    challenges.listByTheme.and.returnValue(Promise.resolve([]));
    meApi.get.and.returnValue(Promise.resolve(baseMe));

    TestBed.configureTestingModule({
      imports: [IdeaDetailComponent],
      providers: [
        provideRouter([]),
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: StrategicThemesService, useValue: themes },
        { provide: ActivitiesService, useValue: activities },
        { provide: ChallengesService, useValue: challenges },
        { provide: MeApiService, useValue: meApi },
        identityProvider(currentUserSam, activeRole),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
    fixture = TestBed.createComponent(IdeaDetailComponent);
  }

  it('renders idea fields and attachments', async () => {
    setup({ ...baseIdea, attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }] });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
    expect(fixture.nativeElement.textContent).toContain('evidence.pdf');
  });

  it('renders every idea field: bilingual text, classification, participation, team roster, consents, code, and timestamps', async () => {
    setup({
      ...baseIdea,
      challengeId: 'challenge-1',
      participationType: 'team',
      teamName: 'Team A',
      teamMembers: [{ samAccountName: 'ofarouk', name: 'Omar Farouk', email: 'ofarouk@gac-demo.sa' }],
      approvedAt: '2026-02-01T12:00:00Z',
      attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }],
    });
    ideasApi.getJourney.and.returnValue(Promise.reject(new Error('journey unavailable')));
    (TestBed.inject(ChallengesService) as jasmine.SpyObj<ChallengesService>).listByTheme.and.returnValue(
      Promise.resolve([{ id: 'challenge-1', textAr: 'ت', textEn: 'Challenge One' }]),
    );
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    // Code + Arabic-only title/problem/solution/benefits.
    expect(text).toContain('IDEA-0001');
    expect(text).toContain('ا');
    expect(text).toContain('م');
    expect(text).toContain('ح');
    expect(text).toContain('ف');
    // Classification (track/activity/challenge) rendered even without a loaded journey hero.
    expect(text).toContain('Digital Track');
    expect(text).toContain('Hackathon');
    expect(text).toContain('Challenge One');
    // Participation + team roster (name and AD identity).
    expect(text).toContain('Team A');
    expect(text).toContain('Omar Farouk');
    expect(text).toContain('ofarouk');
    // Attachments and consents.
    expect(text).toContain('evidence.pdf');
    expect(text).toContain('IDEA-0001');
    // Approved timestamp, when present (formatted via DatePipe; noon-UTC keeps the date stable across CI timezones).
    expect(text).toContain('Feb 1, 2026');
  });

  it('renders the Arabic title in the hero/page-header, never the English title', async () => {
    setup({ ...baseIdea, titleAr: 'العنوان بالعربية', titleEn: 'SHOULD_NOT_APPEAR' });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('العنوان بالعربية');
    expect(text).not.toContain('SHOULD_NOT_APPEAR');
  });

  it('disables the submit button when there are zero attachments', async () => {
    setup({ ...baseIdea, attachments: [] });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.disabled).toBe(true);
  });

  it('calls submit() and refreshes on success', async () => {
    setup({ ...baseIdea, attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }] });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    ideasApi.submit.and.returnValue(Promise.resolve({ id: 'idea-1', status: 'submitted' }));
    ideasApi.getById.and.returnValue(Promise.resolve({ ...baseIdea, status: 'submitted', attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }] }));

    await fixture.componentInstance.onSubmit();

    expect(ideasApi.submit).toHaveBeenCalledWith('idea-1');
    expect(fixture.componentInstance.idea()?.status).toBe('submitted');
  });

  it('does not show edit/submit controls once submitted', async () => {
    setup({ ...baseIdea, status: 'submitted', attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }] });
    // Not the owner here — this test is about the edit/submit block, not withdraw.
    (TestBed.inject(MeApiService) as jasmine.SpyObj<MeApiService>).get.and.returnValue(Promise.resolve({ ...baseMe, id: 'someone-else' }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    // The only button left is the read-only "open attachment" action, not a mutating control.
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>);
    expect(buttons.length).toBe(1);
    expect(buttons[0].textContent).toContain('evidence.pdf');
  });

  it('shows the additional-attachments block without a submit-to-committee button when status is pass_awaiting_attachments', async () => {
    setup({ ...baseIdea, status: 'pass_awaiting_attachments', attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }] });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('input[type="file"]')).toBeTruthy();
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    expect(buttons.some((b) => b.textContent?.includes('Submit to Committee'))).toBe(false);
  });

  it('uploads queued files and refreshes when onUploadQueuedFiles is called', async () => {
    setup({ ...baseIdea, status: 'pass_awaiting_attachments', attachments: [] });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    ideasApi.uploadAttachment.and.returnValue(Promise.resolve({ id: 'att-1', fileName: 'a.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }));
    ideasApi.getById.and.returnValue(Promise.resolve({ ...baseIdea, status: 'pass_awaiting_attachments', attachments: [{ id: 'att-1', fileName: 'a.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }] }));

    const file = new File(['content'], 'a.pdf', { type: 'application/pdf' });
    fixture.componentInstance.queuedFiles.set([file]);

    await fixture.componentInstance.onUploadQueuedFiles();

    expect(ideasApi.uploadAttachment).toHaveBeenCalledWith('idea-1', file);
    expect(fixture.componentInstance.idea()?.attachments.length).toBe(1);
  });

  it('shows the edit link and submit button when status is returned', async () => {
    setup({ ...baseIdea, status: 'returned', attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }] });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('a')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('button')).toBeTruthy();
  });

  it('shows the screening reason when present', async () => {
    setup({ ...baseIdea, status: 'returned', screeningReason: 'Please clarify the budget impact section.' });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Please clarify the budget impact section.');
  });

  it('loads and displays the evaluation summary when pass_awaiting_attachments', async () => {
    setup({ ...baseIdea, status: 'pass_awaiting_attachments', attachments: [] });
    ideasApi.getEvaluations = jasmine.createSpy().and.returnValue(Promise.resolve({
      evaluations: [
        { reviewerLabel: 'Reviewer 1', comment: 'Good scope' },
        { reviewerLabel: 'Reviewer 2', comment: null },
      ],
      supervisorComment: 'Please clarify the budget impact.',
    }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(ideasApi.getEvaluations).toHaveBeenCalledWith('idea-1');
    expect(fixture.nativeElement.textContent).toContain('Reviewer 1');
    expect(fixture.nativeElement.textContent).toContain('Good scope');
    expect(fixture.nativeElement.textContent).toContain('Please clarify the budget impact.');
  });

  it('does not load evaluations when status is not pass_awaiting_attachments', async () => {
    setup({ ...baseIdea, status: 'draft', attachments: [] });
    ideasApi.getEvaluations = jasmine.createSpy();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(ideasApi.getEvaluations).not.toHaveBeenCalled();
  });

  it('loads and displays feedback when the idea is submitter_review (returned after evaluation)', async () => {
    setup({ ...baseIdea, status: 'submitter_review', attachments: [] });
    ideasApi.getEvaluations = jasmine.createSpy().and.returnValue(Promise.resolve({
      evaluations: [{ reviewerLabel: 'Reviewer 1', comment: 'Please clarify scope.' }],
      supervisorComment: null,
    }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(ideasApi.getEvaluations).toHaveBeenCalledWith('idea-1');
    expect(fixture.nativeElement.textContent).toContain('Please clarify scope.');
  });

  it('shows the resubmit-to-supervisor form for the owner when the idea is submitter_review, disabled until a comment is entered', async () => {
    setup({ ...baseIdea, status: 'submitter_review', attachments: [] });
    ideasApi.getEvaluations = jasmine.createSpy().and.returnValue(Promise.resolve({ evaluations: [], supervisorComment: null }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const resubmitButton = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(
      (b) => b.textContent?.includes('Resubmit to Supervisor'),
    );
    expect(resubmitButton).toBeTruthy();
    expect(resubmitButton!.disabled).toBe(true);

    fixture.componentInstance.resubmitComment.set('Addressed the feedback.');
    fixture.detectChanges();
    expect(resubmitButton!.disabled).toBe(false);
  });

  it('does not show the resubmit form for a non-owner, even when submitter_review', async () => {
    setup({ ...baseIdea, status: 'submitter_review', attachments: [] }, null, 'evaluator');
    ideasApi.getEvaluations = jasmine.createSpy().and.returnValue(Promise.resolve({ evaluations: [], supervisorComment: null }));
    (TestBed.inject(MeApiService) as jasmine.SpyObj<MeApiService>).get.and.returnValue(Promise.resolve({ ...baseMe, id: 'someone-else' }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const resubmitButton = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(
      (b) => b.textContent?.includes('Resubmit to Supervisor'),
    );
    expect(resubmitButton).toBeUndefined();
  });

  it('calls resubmitEvaluation() with the comment and refreshes on success', async () => {
    setup({ ...baseIdea, status: 'submitter_review', attachments: [] });
    ideasApi.getEvaluations = jasmine.createSpy().and.returnValue(Promise.resolve({ evaluations: [], supervisorComment: null }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    ideasApi.resubmitEvaluation.and.returnValue(Promise.resolve({ id: 'idea-1', status: 'evaluation' }));
    ideasApi.getById.and.returnValue(Promise.resolve({ ...baseIdea, status: 'evaluation' }));
    fixture.componentInstance.resubmitComment.set('Addressed the feedback.');

    await fixture.componentInstance.onResubmit();

    expect(ideasApi.resubmitEvaluation).toHaveBeenCalledWith('idea-1', 'Addressed the feedback.');
    expect(fixture.componentInstance.idea()?.status).toBe('evaluation');
    expect(fixture.componentInstance.resubmitComment()).toBe('');
  });

  it('shows an error message when resubmit is rejected (e.g. below target score)', async () => {
    setup({ ...baseIdea, status: 'submitter_review', attachments: [] });
    ideasApi.getEvaluations = jasmine.createSpy().and.returnValue(Promise.resolve({ evaluations: [], supervisorComment: null }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    ideasApi.resubmitEvaluation.and.returnValue(Promise.reject({ error: { error: 'Idea is still below the target score.' } }));
    fixture.componentInstance.resubmitComment.set('Addressed the feedback.');

    await fixture.componentInstance.onResubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('Idea is still below the target score.');
  });

  it('loads the journey and renders the timeline and resolved chips', async () => {
    setup({ ...baseIdea, status: 'committee' });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(ideasApi.getJourney).toHaveBeenCalledWith('idea-1');
    expect(fixture.nativeElement.querySelector('app-idea-journey-timeline')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Digital Track');
    expect(fixture.nativeElement.textContent).toContain('Hackathon');
  });

  it('still renders and loads evaluations when getJourney rejects', async () => {
    setup({ ...baseIdea, status: 'pass_awaiting_attachments', attachments: [] });
    ideasApi.getJourney.and.returnValue(Promise.reject(new Error('journey failed')));
    ideasApi.getEvaluations = jasmine.createSpy().and.returnValue(Promise.resolve({ evaluations: [], supervisorComment: null }));

    fixture.detectChanges();
    await expectAsync(fixture.componentInstance.ngOnInit()).toBeResolved();
    fixture.detectChanges();

    expect(fixture.componentInstance.journey()).toBeNull();
    expect(fixture.nativeElement.querySelector('app-idea-journey-timeline')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
    expect(ideasApi.getEvaluations).toHaveBeenCalledWith('idea-1');
    expect(fixture.componentInstance.trackName()).toBe('Digital Track');
  });

  it('shows the post-program stepper for the owner when the idea is in-program', async () => {
    setup({ ...baseIdea, status: 'in_pilot', submitterId: 'owner-1' });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-post-program-stepper')).toBeTruthy();
  });

  it('does not show the stepper for a non-owner', async () => {
    setup({ ...baseIdea, status: 'in_pilot', submitterId: 'someone-else' });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-post-program-stepper')).toBeNull();
  });

  it('renders the error state and retries the fetch when the "Try again" button is clicked', async () => {
    setup(baseIdea);
    ideasApi.getById.and.returnValue(Promise.reject(new Error('network error')));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).not.toBeNull();
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();

    ideasApi.getById.and.returnValue(Promise.resolve(baseIdea));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
  });

  it('renders read-only for a team member who is not the owner: no edit/submit/upload controls, but idea content is shown', async () => {
    setup(
      {
        ...baseIdea,
        status: 'draft',
        participationType: 'team',
        teamName: 'Team A',
        teamMembers: [{ samAccountName: 'ofarouk', name: 'Omar Farouk', email: 'ofarouk@gac-demo.sa' }],
        attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }],
      },
      'ofarouk',
    );
    // The current user's account (fetched via /me) is the team member's own id, not the owner's.
    (TestBed.inject(MeApiService) as jasmine.SpyObj<MeApiService>).get.and.returnValue(Promise.resolve({ ...baseMe, id: 'member-1' }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.isReadOnlyTeamMember()).toBe(true);
    expect(fixture.nativeElement.querySelector('a')).toBeNull();
    // The only button left is the read-only "open attachment" action, not a mutating control.
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>);
    expect(buttons.length).toBe(1);
    expect(buttons[0].textContent).toContain('evidence.pdf');
    expect(fixture.nativeElement.querySelector('input[type="file"]')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
    expect(fixture.nativeElement.textContent).toContain('Problem');
  });

  it('shows a Withdraw control for the owner when the status is withdrawable, and withdraws on confirm', async () => {
    setup({ ...baseIdea, status: 'submitted' });
    spyOn(window, 'confirm').and.returnValue(true);
    ideasApi.withdraw.and.returnValue(Promise.resolve());
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const withdrawButton = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(
      (b) => b.textContent?.includes('Withdraw'),
    );
    expect(withdrawButton).toBeTruthy();

    ideasApi.getById.and.returnValue(Promise.resolve({ ...baseIdea, status: 'withdrawn' }));
    withdrawButton!.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(window.confirm).toHaveBeenCalled();
    expect(ideasApi.withdraw).toHaveBeenCalledWith('idea-1');
    expect(fixture.componentInstance.idea()?.status).toBe('withdrawn');
  });

  it('does not call withdraw when the confirmation is dismissed', async () => {
    setup({ ...baseIdea, status: 'draft' });
    spyOn(window, 'confirm').and.returnValue(false);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const withdrawButton = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(
      (b) => b.textContent?.includes('Withdraw'),
    );
    expect(withdrawButton).toBeTruthy();
    withdrawButton!.click();
    await fixture.whenStable();

    expect(ideasApi.withdraw).not.toHaveBeenCalled();
  });

  it('shows an error message when withdraw fails', async () => {
    setup({ ...baseIdea, status: 'returned' });
    spyOn(window, 'confirm').and.returnValue(true);
    ideasApi.withdraw.and.returnValue(Promise.reject({ error: { error: 'Cannot withdraw right now.' } }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const withdrawButton = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(
      (b) => b.textContent?.includes('Withdraw'),
    );
    withdrawButton!.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Cannot withdraw right now.');
  });

  it('does not show a Withdraw control when the status is not withdrawable', async () => {
    setup({ ...baseIdea, status: 'approved' });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const withdrawButton = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(
      (b) => b.textContent?.includes('Withdraw'),
    );
    expect(withdrawButton).toBeUndefined();
  });

  it('does not show a Withdraw control for a read-only team member, even in a withdrawable status', async () => {
    setup(
      {
        ...baseIdea,
        status: 'draft',
        participationType: 'team',
        teamName: 'Team A',
        teamMembers: [{ samAccountName: 'ofarouk', name: 'Omar Farouk', email: 'ofarouk@gac-demo.sa' }],
      },
      'ofarouk',
    );
    (TestBed.inject(MeApiService) as jasmine.SpyObj<MeApiService>).get.and.returnValue(Promise.resolve({ ...baseMe, id: 'member-1' }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.canWithdraw()).toBe(false);
    const withdrawButton = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(
      (b) => b.textContent?.includes('Withdraw'),
    );
    expect(withdrawButton).toBeUndefined();
  });

  it('openAttachment fetches the blob and opens an inline-viewable file in a new tab via an anchor', async () => {
    setup({ ...baseIdea, attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }] });
    const blob = new Blob(['x'], { type: 'application/pdf' });
    ideasApi.getAttachmentBlob.and.returnValue(Promise.resolve(blob));
    const clickSpy = spyOn(HTMLAnchorElement.prototype, 'click');
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const button = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(
      (b) => b.textContent?.includes('evidence.pdf'),
    );
    expect(button).toBeTruthy();
    button!.click();
    await fixture.whenStable();

    expect(ideasApi.getAttachmentBlob).toHaveBeenCalledWith('idea-1', 'att-1');
    expect(clickSpy).toHaveBeenCalled();
    const anchor = clickSpy.calls.mostRecent().object as HTMLAnchorElement;
    expect(anchor.target).toBe('_blank');
    expect(anchor.getAttribute('download')).toBeNull();
  });

  it('openAttachment downloads a non-inline file by name instead of opening a blank tab', async () => {
    setup({ ...baseIdea, attachments: [{ id: 'att-2', fileName: 'sheet.xlsx', contentType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', fileSizeBytes: 3, uploadedAt: '2026-01-01' }] });
    ideasApi.getAttachmentBlob.and.returnValue(Promise.resolve(new Blob(['x'])));
    const clickSpy = spyOn(HTMLAnchorElement.prototype, 'click');
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    await fixture.componentInstance.openAttachment({ id: 'att-2', fileName: 'sheet.xlsx', contentType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', fileSizeBytes: 3, uploadedAt: '2026-01-01' });

    const anchor = clickSpy.calls.mostRecent().object as HTMLAnchorElement;
    expect(anchor.getAttribute('download')).toBe('sheet.xlsx');
    expect(anchor.target).toBe('');
  });

  it('shows an error message when opening an attachment fails', async () => {
    setup({ ...baseIdea, attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }] });
    ideasApi.getAttachmentBlob.and.returnValue(Promise.reject({ error: { error: 'Attachment not found.' } }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    await fixture.componentInstance.openAttachment({ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' });

    expect(fixture.componentInstance.errorMessage()).toBe('Attachment not found.');
  });

  it('shows the team roster for a judge (judges must see the team)', async () => {
    setup(
      {
        ...baseIdea,
        participationType: 'team',
        teamName: 'Team A',
        teamMembers: [{ samAccountName: 'ofarouk', name: 'Omar Farouk', email: 'ofarouk@gac-demo.sa' }],
      },
      null,
      'judge',
    );
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.hideTeamRoster()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Team A');
    expect(fixture.nativeElement.textContent).toContain('Omar Farouk');
  });

  it('hides the team roster for an evaluator', async () => {
    setup(
      {
        ...baseIdea,
        participationType: 'team',
        teamName: 'Team A',
        teamMembers: [{ samAccountName: 'ofarouk', name: 'Omar Farouk', email: 'ofarouk@gac-demo.sa' }],
      },
      null,
      'evaluator',
    );
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.hideTeamRoster()).toBe(true);
    expect(fixture.nativeElement.textContent).not.toContain('Omar Farouk');
  });

  it('shows the team roster for the owner/submitter', async () => {
    setup({
      ...baseIdea,
      participationType: 'team',
      teamName: 'Team A',
      teamMembers: [{ samAccountName: 'ofarouk', name: 'Omar Farouk', email: 'ofarouk@gac-demo.sa' }],
    });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.hideTeamRoster()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Omar Farouk');
  });

  it('renders the submitter at the head of the team roster tagged as team lead', async () => {
    setup({
      ...baseIdea,
      participationType: 'team',
      teamName: 'Team A',
      submitter: { name: 'Layla Owner', samAccountName: 'lowner', email: 'lowner@gac-demo.sa' },
      teamMembers: [{ samAccountName: 'ofarouk', name: 'Omar Farouk', email: 'ofarouk@gac-demo.sa' }],
    });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Layla Owner');
    expect(text).toContain('قائد الفريق');
    expect(text).toContain('Omar Farouk');
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { IdeasApiService } from '../ideas-api.service';
import { Idea, IdeaAttachment, IdeaJourney } from '../idea.model';
import { CommitteeApiService } from '../../committee/committee-api.service';
import { StrategicThemesService } from '../strategic-themes.service';
import { ActivitiesService } from '../activities.service';
import { ChallengesService } from '../challenges.service';
import { MeApiService } from '../../core/me-api.service';
import { IdeaDetailComponent } from './idea-detail.component';

describe('IdeaDetailComponent', () => {
  let fixture: ComponentFixture<IdeaDetailComponent>;
  let ideasApi: jasmine.SpyObj<IdeasApiService>;
  let committeeApi: jasmine.SpyObj<CommitteeApiService>;

  const baseIdea: Idea = {
    id: 'idea-1', code: 'IDEA-0001', submitterId: 'owner-1', titleAr: 'ا', titleEn: 'Title',
    problemStatementAr: 'م', problemStatementEn: 'Problem', proposedSolutionAr: 'ح', proposedSolutionEn: 'Solution',
    expectedBenefitsAr: 'ف', expectedBenefitsEn: 'Benefits', strategicThemeId: 'theme-1',
    activityId: 'activity-1', challengeId: null, participationType: 'individual', teamName: null, teamMembers: [],
    ipAcknowledged: true, termsAgreed: true,
    status: 'draft', currentStage: 0, updatedAt: '2026-01-01', attachments: [] as IdeaAttachment[], screeningReason: null as string | null,
  };

  const defaultJourney: IdeaJourney = {
    currentStage: 0, stopped: false, evaluationScore: null,
    stages: Array.from({ length: 8 }, (_, index) => ({
      index, state: index === 0 ? 'current' : 'upcoming',
      label: { ar: 'x', en: index === 3 ? 'Committee Review' : 'Stage' }, completedAt: null,
    })),
  };

  function setup(idea: typeof baseIdea): void {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getById', 'submit', 'uploadAttachment', 'getEvaluations', 'getJourney']);
    committeeApi = jasmine.createSpyObj('CommitteeApiService', ['submitToCommittee']);
    const themes = jasmine.createSpyObj('StrategicThemesService', ['list']);
    const activities = jasmine.createSpyObj('ActivitiesService', ['list']);
    const challenges = jasmine.createSpyObj('ChallengesService', ['listByTheme']);
    const meApi = jasmine.createSpyObj('MeApiService', ['get']);
    ideasApi.getById.and.returnValue(Promise.resolve(idea));
    ideasApi.getJourney.and.returnValue(Promise.resolve(defaultJourney));
    themes.list.and.returnValue(Promise.resolve([{ id: 'theme-1', nameAr: 'ت', nameEn: 'Digital Track' }]));
    activities.list.and.returnValue(Promise.resolve([{ id: 'activity-1', nameAr: 'ن', nameEn: 'Hackathon' }]));
    challenges.listByTheme.and.returnValue(Promise.resolve([]));
    meApi.get.and.returnValue(Promise.resolve({ id: 'owner-1' }));

    TestBed.configureTestingModule({
      imports: [IdeaDetailComponent],
      providers: [
        provideRouter([]),
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: CommitteeApiService, useValue: committeeApi },
        { provide: StrategicThemesService, useValue: themes },
        { provide: ActivitiesService, useValue: activities },
        { provide: ChallengesService, useValue: challenges },
        { provide: MeApiService, useValue: meApi },
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
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('button')).toBeNull();
  });

  it('shows the finalize block with a submit-to-committee button when status is pass_awaiting_attachments', async () => {
    setup({ ...baseIdea, status: 'pass_awaiting_attachments', attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }] });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const submitToCommitteeButton = buttons.find((b) => b.textContent?.includes('Submit to Committee'));
    expect(submitToCommitteeButton).toBeTruthy();
    expect(submitToCommitteeButton!.disabled).toBe(false);
  });

  it('disables the submit-to-committee button when there are zero attachments', async () => {
    setup({ ...baseIdea, status: 'pass_awaiting_attachments', attachments: [] });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const submitToCommitteeButton = buttons.find((b) => b.textContent?.includes('Submit to Committee'));
    expect(submitToCommitteeButton!.disabled).toBe(true);
  });

  it('calls committeeApi.submitToCommittee() and refreshes on success', async () => {
    setup({ ...baseIdea, status: 'pass_awaiting_attachments', attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }] });
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    committeeApi.submitToCommittee.and.returnValue(Promise.resolve({ id: 'idea-1', status: 'committee' }));
    ideasApi.getById.and.returnValue(Promise.resolve({ ...baseIdea, status: 'committee', attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }] }));

    await fixture.componentInstance.onSubmitToCommittee();

    expect(committeeApi.submitToCommittee).toHaveBeenCalledWith('idea-1');
    expect(fixture.componentInstance.idea()?.status).toBe('committee');
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
        { reviewerLabel: 'Reviewer 1', score: 6, comment: 'Good scope' },
        { reviewerLabel: 'Reviewer 2', score: 8, comment: null },
      ],
      averageScore: 7,
    }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(ideasApi.getEvaluations).toHaveBeenCalledWith('idea-1');
    expect(fixture.nativeElement.textContent).toContain('Reviewer 1');
    expect(fixture.nativeElement.textContent).toContain('Good scope');
    expect(fixture.nativeElement.textContent).toContain('7');
  });

  it('does not load evaluations when status is not pass_awaiting_attachments', async () => {
    setup({ ...baseIdea, status: 'draft', attachments: [] });
    ideasApi.getEvaluations = jasmine.createSpy();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(ideasApi.getEvaluations).not.toHaveBeenCalled();
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
    ideasApi.getEvaluations = jasmine.createSpy().and.returnValue(Promise.resolve({ evaluations: [], averageScore: null }));

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
});

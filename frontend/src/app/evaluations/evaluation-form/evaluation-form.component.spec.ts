import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { EvaluationsApiService } from '../evaluations-api.service';
import { EvaluationCriterion, MyEvaluation } from '../evaluation.model'; // Change 20260726
import { IdeasApiService } from '../../ideas/ideas-api.service';
import { StrategicThemesService } from '../../ideas/strategic-themes.service';
import { ActivitiesService } from '../../ideas/activities.service';
import { ChallengesService } from '../../ideas/challenges.service';
import { Idea, IdeaAttachment } from '../../ideas/idea.model';
import { EvaluationFormComponent } from './evaluation-form.component';

describe('EvaluationFormComponent', () => {
  let fixture: ComponentFixture<EvaluationFormComponent>;
  let evaluationsApi: jasmine.SpyObj<EvaluationsApiService>;
  let ideasApi: jasmine.SpyObj<IdeasApiService>;
  let themesApi: jasmine.SpyObj<StrategicThemesService>;
  let activitiesApi: jasmine.SpyObj<ActivitiesService>;
  let challengesApi: jasmine.SpyObj<ChallengesService>;
  let router: jasmine.SpyObj<Router>;

  // Change 20260726 — deliberately 3 criteria (not the 5 legacy ones) so the form
  // is proven to be driven by the API rather than by hardcoded fields.
  const baseCriteria: EvaluationCriterion[] = [
    { code: 'innovation', nameAr: 'الابتكار', nameEn: 'Innovation', descriptionAr: null, descriptionEn: null, weight: 0.5, sortOrder: 1 },
    { code: 'impact', nameAr: 'الأثر', nameEn: 'Impact', descriptionAr: null, descriptionEn: null, weight: 0.3, sortOrder: 2 },
    { code: 'feasibility', nameAr: 'الجدوى', nameEn: 'Feasibility', descriptionAr: null, descriptionEn: null, weight: 0.2, sortOrder: 3 },
  ];

  const baseIdea: Idea = {
    id: 'idea-1', code: 'IDEA-0001', submitterId: 'owner-1', titleAr: 'عنوان الفكرة', titleEn: 'Idea Title',
    problemStatementAr: 'بيان المشكلة', problemStatementEn: 'Problem', proposedSolutionAr: 'الحل المقترح', proposedSolutionEn: 'Solution',
    expectedBenefitsAr: 'الفوائد المتوقعة', expectedBenefitsEn: 'Benefits', strategicThemeId: 'theme-1',
    activityId: 'activity-1', challengeId: null, participationType: 'team', teamName: 'Team A',
    teamMembers: [{ samAccountName: 'ofarouk', name: 'Omar Farouk', email: 'ofarouk@gac-demo.sa' }],
    ipAcknowledged: true, termsAgreed: true,
    status: 'committee', currentStage: 3, createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01', approvedAt: null,
    attachments: [] as IdeaAttachment[], screeningReason: null as string | null, editableSections: null,
  };

  // Change 20260726 — configures the spies without running change detection, so individual
  // tests can override a stub before the component initialises.
  function configure(idea: Idea = baseIdea): void {
    evaluationsApi = jasmine.createSpyObj('EvaluationsApiService', ['submit', 'getCriteria', 'getMine']);
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getById', 'getAttachmentBlob']);
    themesApi = jasmine.createSpyObj('StrategicThemesService', ['list']);
    activitiesApi = jasmine.createSpyObj('ActivitiesService', ['list']);
    challengesApi = jasmine.createSpyObj('ChallengesService', ['listByTheme']);
    router = jasmine.createSpyObj('Router', ['navigate']);

    evaluationsApi.getCriteria.and.returnValue(Promise.resolve(baseCriteria));
    evaluationsApi.getMine.and.returnValue(Promise.resolve([]));
    ideasApi.getById.and.returnValue(Promise.resolve(idea));
    themesApi.list.and.returnValue(Promise.resolve([{ id: 'theme-1', nameAr: 'المسار الرقمي', nameEn: 'Digital Track' }]));
    activitiesApi.list.and.returnValue(Promise.resolve([{ id: 'activity-1', nameAr: 'الهاكاثون', nameEn: 'Hackathon' }]));
    challengesApi.listByTheme.and.returnValue(Promise.resolve([]));

    TestBed.configureTestingModule({
      imports: [EvaluationFormComponent],
      providers: [
        { provide: EvaluationsApiService, useValue: evaluationsApi },
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: StrategicThemesService, useValue: themesApi },
        { provide: ActivitiesService, useValue: activitiesApi },
        { provide: ChallengesService, useValue: challengesApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
  }

  /** Creates the component and lets ngOnInit's async loads settle. */
  // Change 20260726
  async function render(): Promise<void> {
    fixture = TestBed.createComponent(EvaluationFormComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function setup(idea: Idea = baseIdea): Promise<void> {
    configure(idea);
    await render();
  }

  /** Fills every score control so the form is valid. */
  // Change 20260726
  function fillAllScores(score = 7): void {
    fixture.componentInstance.scores.controls.forEach((c) => c.setValue(score));
  }

  function scoreSelects(): HTMLSelectElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('select[data-criterion]') as NodeListOf<HTMLSelectElement>);
  }

  // Change 20260726
  it('renders one score control per criterion returned by the API', async () => {
    await setup();

    expect(fixture.componentInstance.scores.length).toBe(3);
    const selects = scoreSelects();
    expect(selects.length).toBe(3);
    expect(selects.map((s) => s.getAttribute('data-criterion'))).toEqual(['innovation', 'impact', 'feasibility']);
    for (const select of selects) {
      // 11 numeric options (0..10) plus the leading placeholder option.
      expect(select.querySelectorAll('option').length).toBe(12);
    }
  });

  // Change 20260726
  it('labels each criterion with its localised name and weight percentage', async () => {
    await setup();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Innovation (50%)');
    expect(text).toContain('Impact (30%)');
    expect(text).toContain('Feasibility (20%)');
  });

  it('marks the form invalid when a required score field is empty', async () => {
    await setup();
    fillAllScores();
    fixture.componentInstance.scores.at(0).setValue(null);

    expect(fixture.componentInstance.form.invalid).toBe(true);
  });

  // Change 20260726
  it('submits the scores as a dictionary keyed by criterion code, then navigates to the queue', async () => {
    await setup();
    evaluationsApi.submit.and.returnValue(Promise.resolve({ id: 'eval-1', totalScore: 7, recommendation: 'pass', ideaStatus: 'pass_awaiting_attachments' }));

    fixture.componentInstance.scores.at(0).setValue(7);
    fixture.componentInstance.scores.at(1).setValue(8);
    fixture.componentInstance.scores.at(2).setValue(9);
    fixture.componentInstance.form.controls.comments.setValue('Good idea.');
    await fixture.componentInstance.onSubmit();

    expect(evaluationsApi.submit).toHaveBeenCalledWith('idea-1', {
      criteriaScores: { innovation: 7, impact: 8, feasibility: 9 },
      comments: 'Good idea.',
      action: 'submit',
      conflictOfInterest: false,
    });
    expect(router.navigate).toHaveBeenCalledWith(['/evaluations/queue']);
  });

  // Change 20260726
  it('saves a partially completed draft with action "draft" and only the filled scores', async () => {
    await setup();
    evaluationsApi.submit.and.returnValue(Promise.resolve({ id: 'eval-1', totalScore: 0, recommendation: 'pending', ideaStatus: 'under_evaluation', submittedAt: null }));

    fixture.componentInstance.scores.at(0).setValue(6);
    expect(fixture.componentInstance.form.invalid).withContext('draft is deliberately incomplete').toBe(true);

    await fixture.componentInstance.onSaveDraft();

    expect(evaluationsApi.submit).toHaveBeenCalledWith('idea-1', {
      criteriaScores: { innovation: 6 },
      comments: null,
      action: 'draft',
      conflictOfInterest: false,
    });
    expect(router.navigate).toHaveBeenCalledWith(['/evaluations/mine']);
  });

  // Change 20260726
  it('does not submit an incomplete form', async () => {
    await setup();
    fixture.componentInstance.scores.at(0).setValue(6);

    await fixture.componentInstance.onSubmit();

    expect(evaluationsApi.submit).not.toHaveBeenCalled();
  });

  // Change 20260726
  it('sends the conflict-of-interest flag when the checkbox is ticked, keeping scores editable', async () => {
    await setup();
    evaluationsApi.submit.and.returnValue(Promise.resolve({ id: 'eval-1', totalScore: 7, recommendation: 'pass', ideaStatus: 'pass_awaiting_attachments' }));

    const checkbox = fixture.nativeElement.querySelector('input[type="checkbox"][formControlName="conflictOfInterest"]') as HTMLInputElement;
    expect(checkbox).withContext('expected a conflict-of-interest checkbox').toBeTruthy();
    checkbox.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.form.controls.conflictOfInterest.value).toBe(true);
    // The declaration must not lock the evaluator out of scoring.
    expect(scoreSelects().every((s) => !s.disabled)).toBe(true);

    fillAllScores();
    await fixture.componentInstance.onSubmit();

    expect(evaluationsApi.submit).toHaveBeenCalledWith('idea-1', jasmine.objectContaining({ conflictOfInterest: true }));
  });

  // Change 20260726
  it('shows a notice once a conflict of interest is declared', async () => {
    await setup();
    expect(fixture.nativeElement.querySelector('[data-testid="coi-notice"]')).toBeNull();

    fixture.componentInstance.form.controls.conflictOfInterest.setValue(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="coi-notice"]')).toBeTruthy();
  });

  // Change 20260726
  it('shows an error and disables both actions when the criteria cannot be loaded', async () => {
    configure();
    evaluationsApi.getCriteria.and.returnValue(Promise.reject(new Error('network error')));
    await render();

    expect(fixture.componentInstance.criteriaUnavailable()).toBe(true);
    expect(fixture.componentInstance.errorMessage()).toBeTruthy();
    expect(scoreSelects().length).toBe(0);

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('form button') as NodeListOf<HTMLButtonElement>);
    expect(buttons.length).toBeGreaterThan(0);
    expect(buttons.every((b) => b.disabled)).toBe(true);

    await fixture.componentInstance.onSubmit();
    await fixture.componentInstance.onSaveDraft();
    expect(evaluationsApi.submit).not.toHaveBeenCalled();
  });

  // Change 20260726
  it('resumes an unsubmitted draft by patching its saved scores, comments and COI flag', async () => {
    configure();
    const draft: MyEvaluation = {
      id: 'eval-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'T', totalScore: 0,
      recommendation: 'pending', submittedAt: null, ideaEnteredEvaluationAt: null,
      criteriaScoresJson: '{"innovation":4,"feasibility":9}', comments: 'Half done.', conflictOfInterest: true,
    };
    evaluationsApi.getMine.and.returnValue(Promise.resolve([draft]));
    await render();

    expect(fixture.componentInstance.resumedDraft()).toBe(true);
    expect(fixture.componentInstance.scores.at(0).value).toBe(4);
    expect(fixture.componentInstance.scores.at(1).value).toBeNull();
    expect(fixture.componentInstance.scores.at(2).value).toBe(9);
    expect(fixture.componentInstance.form.controls.comments.value).toBe('Half done.');
    expect(fixture.componentInstance.form.controls.conflictOfInterest.value).toBe(true);
  });

  // Change 20260726
  it('ignores an already-submitted evaluation instead of resuming it as a draft', async () => {
    configure();
    evaluationsApi.getMine.and.returnValue(Promise.resolve([{
      id: 'eval-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'T', totalScore: 7,
      recommendation: 'pass', submittedAt: '2026-01-01T00:00:00Z', ideaEnteredEvaluationAt: null,
      criteriaScoresJson: '{"innovation":4}', comments: 'Done.', conflictOfInterest: false,
    } as MyEvaluation]));
    await render();

    expect(fixture.componentInstance.resumedDraft()).toBe(false);
    expect(fixture.componentInstance.scores.at(0).value).toBeNull();
  });

  it('shows an inline error message when submission fails', async () => {
    await setup();
    evaluationsApi.submit.and.returnValue(Promise.reject({ error: { error: 'You have already evaluated this idea.' } }));
    fillAllScores();

    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('You have already evaluated this idea.');
  });

  it('selecting a score option sets a numeric (not string) form value', async () => {
    await setup();

    const select = scoreSelects()[0];
    select.value = select.querySelectorAll('option')[9].value; // the option for 8 (index 0 = placeholder, 1 = 0, ..., 9 = 8)
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.componentInstance.scores.at(0).value).toBe(8);
    expect(typeof fixture.componentInstance.scores.at(0).value).toBe('number');
  });

  it("shows the idea's title, problem, solution, benefits, classification and attachments beside the form", async () => {
    await setup({
      ...baseIdea,
      attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }],
    });

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('IDEA-0001');
    expect(text).toContain('عنوان الفكرة');
    expect(text).toContain('بيان المشكلة');
    expect(text).toContain('الحل المقترح');
    expect(text).toContain('الفوائد المتوقعة');
    expect(text).toContain('المسار الرقمي');
    expect(text).toContain('الهاكاثون');
    expect(text).toContain('evidence.pdf');
  });

  it('embeds the shared idea-context-panel in evaluator view mode', async () => {
    await setup();

    const panel = fixture.nativeElement.querySelector('app-idea-context-panel');
    expect(panel).toBeTruthy();
  });

  it('does NOT render team members / team roster on the evaluation screen', async () => {
    await setup();

    const text = fixture.nativeElement.textContent as string;
    expect(text).not.toContain('Omar Farouk');
    expect(text).not.toContain('ofarouk');
    expect(text).not.toContain('Team A');
  });

  it('opens an attachment via getAttachmentBlob and window.open, mirroring idea-detail', async () => {
    await setup({
      ...baseIdea,
      attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }],
    });
    const blob = new Blob(['x'], { type: 'application/pdf' });
    ideasApi.getAttachmentBlob.and.returnValue(Promise.resolve(blob));
    spyOn(window, 'open');

    const button = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find((b) =>
      b.textContent?.includes('evidence.pdf'),
    );
    expect(button).toBeTruthy();
    button!.click();
    await fixture.whenStable();

    expect(ideasApi.getAttachmentBlob).toHaveBeenCalledWith('idea-1', 'att-1');
    expect(window.open).toHaveBeenCalled();
  });

  it('still shows the scoring form when the idea fetch fails, with an info-unavailable note', async () => {
    configure();
    ideasApi.getById.and.returnValue(Promise.reject(new Error('network error')));
    await render();

    expect(fixture.componentInstance.ideaInfoUnavailable()).toBe(true);
    expect(fixture.nativeElement.querySelector('form')).toBeTruthy();
    expect(scoreSelects().length).toBe(3); // Change 20260726
  });
});

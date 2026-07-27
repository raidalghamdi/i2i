import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { CommitteeApiService } from '../committee-api.service';
import { IdeasApiService } from '../../ideas/ideas-api.service';
import { Idea, IdeaAttachment } from '../../ideas/idea.model';
import { EvaluationReviewApiService } from '../../evaluations/evaluation-review-api.service';
import { EvaluationReviewDetail } from '../../evaluations/evaluation-review.model';
import { CommitteeDecisionFormComponent } from './committee-decision-form.component';

describe('CommitteeDecisionFormComponent', () => {
  let fixture: ComponentFixture<CommitteeDecisionFormComponent>;
  let committeeApi: jasmine.SpyObj<CommitteeApiService>;
  let ideasApi: jasmine.SpyObj<IdeasApiService>;
  let evaluationReviewApi: jasmine.SpyObj<EvaluationReviewApiService>;
  let router: jasmine.SpyObj<Router>;

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

  const baseDetail: EvaluationReviewDetail = {
    evaluations: [{ reviewerLabel: 'Reviewer 1', score: 8, comment: 'Solid proposal.' }],
    aggregateScore: 8,
    aggregateByCriteria: { originality: 8 },
    supervisorComment: null,
  };

  function setup(): void {
    committeeApi = jasmine.createSpyObj('CommitteeApiService', ['getCriteria', 'submitDecision']);
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getById']);
    evaluationReviewApi = jasmine.createSpyObj('EvaluationReviewApiService', ['getDetail']);
    router = jasmine.createSpyObj('Router', ['navigate']);
    committeeApi.getCriteria.and.returnValue(Promise.resolve([
      { code: 'originality', nameAr: 'أ', nameEn: 'Originality', weight: 0.30 },
      { code: 'feasibility', nameAr: 'ب', nameEn: 'Feasibility', weight: 0.25 },
      { code: 'impact', nameAr: 'ج', nameEn: 'Impact', weight: 0.30 },
      { code: 'alignment', nameAr: 'د', nameEn: 'Alignment', weight: 0.15 },
    ]));
    ideasApi.getById.and.returnValue(Promise.resolve(baseIdea));
    evaluationReviewApi.getDetail.and.returnValue(Promise.resolve(baseDetail));

    TestBed.configureTestingModule({
      imports: [CommitteeDecisionFormComponent],
      providers: [
        { provide: CommitteeApiService, useValue: committeeApi },
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: EvaluationReviewApiService, useValue: evaluationReviewApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
    fixture = TestBed.createComponent(CommitteeDecisionFormComponent);
  }

  // Sets each named control individually via `.get(name)?.setValue(...)` rather than a single
  // `form.patchValue({...})` call. The dynamically-added criterion controls (originality, feasibility,
  // etc.) aren't part of the FormGroup's statically-inferred value type (only `decisionTypeCode`/`comments`
  // are, from the `fb.group({...})` call in the component) — passing an object literal mixing static and
  // dynamic keys directly to `patchValue` would fail TypeScript's excess-property check. `.get(name)?.setValue(...)`
  // has no such static shape constraint.
  function setFormValues(form: typeof fixture.componentInstance.form, values: Record<string, unknown>): void {
    for (const [key, value] of Object.entries(values)) {
      form.get(key)?.setValue(value);
    }
  }

  it('marks the form invalid until all criteria and a decision type are filled', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.form.invalid).toBe(true);
  });

  it('submits all criteria scores, decision type, and comments, then navigates to the queue', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    committeeApi.submitDecision.and.returnValue(Promise.resolve({ id: 'decision-1', totalScore: 8, ideaStatus: 'committee' }));

    setFormValues(fixture.componentInstance.form, {
      originality: 8, feasibility: 8, impact: 8, alignment: 8,
      decisionTypeCode: 'approved', comments: 'Great idea.',
    });
    await fixture.componentInstance.onSubmit();

    expect(committeeApi.submitDecision).toHaveBeenCalledWith('idea-1', {
      decisionTypeCode: 'approved',
      criteriaScores: { originality: 8, feasibility: 8, impact: 8, alignment: 8 },
      comments: 'Great idea.',
    }, []); // Change 20260726
    expect(router.navigate).toHaveBeenCalledWith(['/committee/queue']);
  });

  // Change 20260726
  describe('attachments', () => {
    function pdf(name = 'minutes.pdf', size = 1024): File {
      const file = new File(['x'], name, { type: 'application/pdf' });
      Object.defineProperty(file, 'size', { value: size });
      return file;
    }

    function selectFiles(files: File[]): void {
      fixture.componentInstance.onFilesSelected({
        target: { files, value: 'C:\\fakepath\\x' } as unknown as HTMLInputElement,
      } as unknown as Event);
    }

    it('queues files that pass the type and size checks', async () => {
      setup();
      fixture.detectChanges();
      await fixture.componentInstance.ngOnInit();

      selectFiles([pdf('a.pdf'), pdf('b.pdf')]);

      expect(fixture.componentInstance.queuedFiles().map((f) => f.name)).toEqual(['a.pdf', 'b.pdf']);
      expect(fixture.componentInstance.attachmentError()).toBeNull();
    });

    it('rejects a file whose MIME type is not on the allowlist', async () => {
      setup();
      fixture.detectChanges();
      await fixture.componentInstance.ngOnInit();

      selectFiles([new File(['x'], 'evil.exe', { type: 'application/x-msdownload' })]);

      expect(fixture.componentInstance.queuedFiles().length).toBe(0);
      expect(fixture.componentInstance.attachmentError()).toContain("isn't allowed");
    });

    it('rejects a file larger than 10MB but keeps the acceptable ones', async () => {
      setup();
      fixture.detectChanges();
      await fixture.componentInstance.ngOnInit();

      selectFiles([pdf('huge.pdf', 10 * 1024 * 1024 + 1), pdf('ok.pdf')]);

      expect(fixture.componentInstance.queuedFiles().map((f) => f.name)).toEqual(['ok.pdf']);
      expect(fixture.componentInstance.attachmentError()).toContain('10MB');
    });

    it('removes a queued file by index', async () => {
      setup();
      fixture.detectChanges();
      await fixture.componentInstance.ngOnInit();
      selectFiles([pdf('a.pdf'), pdf('b.pdf')]);

      fixture.componentInstance.removeQueuedFile(0);

      expect(fixture.componentInstance.queuedFiles().map((f) => f.name)).toEqual(['b.pdf']);
    });

    it('lists the queued file names with a remove button before submit', async () => {
      setup();
      fixture.detectChanges();
      await fixture.componentInstance.ngOnInit();
      fixture.detectChanges();
      selectFiles([pdf('minutes.pdf')]);
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent as string).toContain('minutes.pdf');
      expect(fixture.nativeElement.querySelector('[aria-label="Remove attachment"]')).toBeTruthy();
    });

    it('passes the queued files to the API on submit', async () => {
      setup();
      fixture.detectChanges();
      await fixture.componentInstance.ngOnInit();
      committeeApi.submitDecision.and.returnValue(Promise.resolve({ id: 'decision-1', totalScore: 8, ideaStatus: 'committee' }));
      const file = pdf('minutes.pdf');
      selectFiles([file]);

      setFormValues(fixture.componentInstance.form, {
        originality: 8, feasibility: 8, impact: 8, alignment: 8,
        decisionTypeCode: 'approved', comments: null,
      });
      await fixture.componentInstance.onSubmit();

      expect(committeeApi.submitDecision).toHaveBeenCalledWith('idea-1', jasmine.any(Object), [file]);
    });
  });

  it('loads and shows the idea and the evaluator rating (via the shared idea-context-panel) above the criteria form', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(ideasApi.getById).toHaveBeenCalledWith('idea-1');
    expect(evaluationReviewApi.getDetail).toHaveBeenCalledWith('idea-1');
    const panel = fixture.nativeElement.querySelector('app-idea-context-panel');
    expect(panel).toBeTruthy();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('عنوان الفكرة');
    // Judges are elevated: the team is visible.
    expect(text).toContain('Omar Farouk');
    // The evaluator rating shows for the judge.
    expect(text).toContain('Reviewer 1');
    expect(text).toContain('Solid proposal.');
  });

  it('shows an inline error message when submission fails', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    committeeApi.submitDecision.and.returnValue(Promise.reject({ error: { error: 'You have already decided on this idea.' } }));

    setFormValues(fixture.componentInstance.form, {
      originality: 8, feasibility: 8, impact: 8, alignment: 8,
      decisionTypeCode: 'approved', comments: null,
    });
    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('You have already decided on this idea.');
  });

  it('shows the error state and retries the fetch when "Try again" is clicked', async () => {
    committeeApi = jasmine.createSpyObj('CommitteeApiService', ['getCriteria', 'submitDecision']);
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getById']);
    evaluationReviewApi = jasmine.createSpyObj('EvaluationReviewApiService', ['getDetail']);
    router = jasmine.createSpyObj('Router', ['navigate']);
    committeeApi.getCriteria.and.returnValue(Promise.reject(new Error('boom')));
    ideasApi.getById.and.returnValue(Promise.resolve(baseIdea));
    evaluationReviewApi.getDetail.and.returnValue(Promise.resolve(baseDetail));

    TestBed.configureTestingModule({
      imports: [CommitteeDecisionFormComponent],
      providers: [
        { provide: CommitteeApiService, useValue: committeeApi },
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: EvaluationReviewApiService, useValue: evaluationReviewApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
    fixture = TestBed.createComponent(CommitteeDecisionFormComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).not.toBeNull();
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();

    committeeApi.getCriteria.and.returnValue(Promise.resolve([
      { code: 'originality', nameAr: 'أ', nameEn: 'Originality', weight: 0.30 },
    ]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.componentInstance.criteria().length).toBe(1);
  });
});

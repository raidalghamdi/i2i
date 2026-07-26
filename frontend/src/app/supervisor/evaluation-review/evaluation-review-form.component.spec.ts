import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { IdeasApiService } from '../../ideas/ideas-api.service';
import { Idea, IdeaAttachment } from '../../ideas/idea.model';
import { EvaluationReviewApiService } from '../../evaluations/evaluation-review-api.service';
import { EvaluationReviewDetail } from '../../evaluations/evaluation-review.model';
import { EvaluationReviewFormComponent } from './evaluation-review-form.component';

describe('EvaluationReviewFormComponent', () => {
  let fixture: ComponentFixture<EvaluationReviewFormComponent>;
  let ideasApi: jasmine.SpyObj<IdeasApiService>;
  let evaluationReviewApi: jasmine.SpyObj<EvaluationReviewApiService>;
  let router: jasmine.SpyObj<Router>;

  const baseIdea: Idea = {
    id: 'idea-1', code: 'IDEA-0001', submitterId: 'user-1', titleAr: 'ا', titleEn: 'Title',
    problemStatementAr: 'م', problemStatementEn: 'Problem', proposedSolutionAr: 'ح', proposedSolutionEn: 'Solution',
    expectedBenefitsAr: 'ف', expectedBenefitsEn: 'Benefits', strategicThemeId: 'theme-1',
    activityId: 'activity-1', challengeId: null, participationType: 'individual', teamName: null, teamMembers: [],
    ipAcknowledged: true, termsAgreed: true,
    status: 'evaluation', currentStage: 2, createdAt: '2026-01-01', updatedAt: '2026-01-01', approvedAt: null, attachments: [] as IdeaAttachment[], screeningReason: null, editableSections: null,
  };

  const baseDetail: EvaluationReviewDetail = {
    evaluations: [
      { reviewerLabel: 'Reviewer 1', score: 8, comment: 'Solid proposal.' },
      { reviewerLabel: 'Reviewer 2', score: 6, comment: null },
    ],
    aggregateScore: 7,
    aggregateByCriteria: { innovation: 7 },
    supervisorComment: null,
  };

  function setup(): void {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getById']);
    ideasApi.getById.and.returnValue(Promise.resolve(baseIdea));
    evaluationReviewApi = jasmine.createSpyObj('EvaluationReviewApiService', ['getDetail', 'submitDecision']);
    evaluationReviewApi.getDetail.and.returnValue(Promise.resolve(baseDetail));
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [EvaluationReviewFormComponent],
      providers: [
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: EvaluationReviewApiService, useValue: evaluationReviewApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
    fixture = TestBed.createComponent(EvaluationReviewFormComponent);
  }

  it('loads the idea and evaluation detail, and renders evaluator comments explicitly', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('IDEA-0001');
    expect(text).toContain('Reviewer 1');
    expect(text).toContain('Solid proposal.');
    expect(text).toContain('Reviewer 2');
    expect(text).toContain('No comment provided.');
  });

  it('submits a forward decision with the supervisor comment', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    evaluationReviewApi.submitDecision.and.returnValue(Promise.resolve({ id: 'idea-1', status: 'committee' }));
    fixture.componentInstance.form.get('decisionCode')!.setValue('forward');
    fixture.componentInstance.form.get('supervisorComment')!.setValue('Looks ready for committee.');

    await fixture.componentInstance.onSubmit();

    expect(evaluationReviewApi.submitDecision).toHaveBeenCalledWith('idea-1', {
      decisionCode: 'forward',
      supervisorComment: 'Looks ready for committee.',
    });
    expect(router.navigate).toHaveBeenCalledWith(['/supervisor/evaluation-review']);
  });

  it('requires a reason of at least 10 characters before allowing return', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    fixture.componentInstance.form.get('decisionCode')!.setValue('return');
    fixture.componentInstance.form.get('reason')!.setValue('too short');
    expect(fixture.componentInstance.form.invalid).toBe(true);

    fixture.componentInstance.form.get('reason')!.setValue('This needs more detail before resubmission.');
    expect(fixture.componentInstance.form.invalid).toBe(false);
  });

  it('submits a return decision with the selected editableSections and reason', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    evaluationReviewApi.submitDecision.and.returnValue(Promise.resolve({ id: 'idea-1', status: 'returned' }));
    fixture.componentInstance.form.get('decisionCode')!.setValue('return');
    fixture.componentInstance.form.get('reason')!.setValue('This needs more detail before resubmission.');
    fixture.componentInstance.selectedSections.set(['proposed_solution']);

    await fixture.componentInstance.onSubmit();

    expect(evaluationReviewApi.submitDecision).toHaveBeenCalledWith('idea-1', {
      decisionCode: 'return',
      reason: 'This needs more detail before resubmission.',
      editableSections: ['proposed_solution'],
    });
    expect(router.navigate).toHaveBeenCalledWith(['/supervisor/evaluation-review']);
  });

  it('submits a fail decision with an optional reason', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    evaluationReviewApi.submitDecision.and.returnValue(Promise.resolve({ id: 'idea-1', status: 'evaluation_failed' }));
    fixture.componentInstance.form.get('decisionCode')!.setValue('fail');
    fixture.componentInstance.form.get('reason')!.setValue('Below threshold.');

    await fixture.componentInstance.onSubmit();

    expect(evaluationReviewApi.submitDecision).toHaveBeenCalledWith('idea-1', {
      decisionCode: 'fail',
      reason: 'Below threshold.',
    });
    expect(router.navigate).toHaveBeenCalledWith(['/supervisor/evaluation-review']);
  });

  it('shows an inline error message when submission fails', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    evaluationReviewApi.submitDecision.and.returnValue(Promise.reject({ error: { error: 'You may not decide on this idea.' } }));
    fixture.componentInstance.form.get('decisionCode')!.setValue('fail');

    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('You may not decide on this idea.');
  });

  it('shows an error state with retry when loading fails, and recovers on retry', async () => {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getById']);
    ideasApi.getById.and.returnValue(Promise.reject({ error: { error: 'Idea not found' } }));
    evaluationReviewApi = jasmine.createSpyObj('EvaluationReviewApiService', ['getDetail', 'submitDecision']);
    evaluationReviewApi.getDetail.and.returnValue(Promise.resolve(baseDetail));
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [EvaluationReviewFormComponent],
      providers: [
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: EvaluationReviewApiService, useValue: evaluationReviewApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
    fixture = TestBed.createComponent(EvaluationReviewFormComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Idea not found');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    ideasApi.getById.and.returnValue(Promise.resolve(baseIdea));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
  });
});

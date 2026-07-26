import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { IdeasApiService } from '../../ideas/ideas-api.service';
import { Idea, IdeaAttachment } from '../../ideas/idea.model';
import { SupervisorApiService } from '../supervisor-api.service';
import { RoleUser } from '../supervisor.model';
import { ScreeningDecisionFormComponent } from './screening-decision-form.component';

describe('ScreeningDecisionFormComponent', () => {
  let fixture: ComponentFixture<ScreeningDecisionFormComponent>;
  let ideasApi: jasmine.SpyObj<IdeasApiService>;
  let supervisorApi: jasmine.SpyObj<SupervisorApiService>;
  let router: jasmine.SpyObj<Router>;

  const baseIdea: Idea = {
    id: 'idea-1', code: 'IDEA-0001', submitterId: 'user-1', titleAr: 'ا', titleEn: 'Title',
    problemStatementAr: 'م', problemStatementEn: 'Problem', proposedSolutionAr: 'ح', proposedSolutionEn: 'Solution',
    expectedBenefitsAr: 'ف', expectedBenefitsEn: 'Benefits', strategicThemeId: 'theme-1',
    activityId: 'activity-1', challengeId: null, participationType: 'individual', teamName: null, teamMembers: [],
    ipAcknowledged: true, termsAgreed: true,
    status: 'submitted', currentStage: 1, createdAt: '2026-01-01', updatedAt: '2026-01-01', approvedAt: null, attachments: [] as IdeaAttachment[], screeningReason: null, editableSections: null,
  };

  const evaluators: RoleUser[] = [
    { id: 'eval-1', fullNameAr: 'أحمد', fullNameEn: 'Ahmed' },
    { id: 'eval-2', fullNameAr: 'سارة', fullNameEn: 'Sara' },
  ];

  function setup(): void {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getById']);
    ideasApi.getById.and.returnValue(Promise.resolve(baseIdea));
    supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['submitScreeningDecision', 'getUsersByRole']);
    supervisorApi.getUsersByRole.and.returnValue(Promise.resolve(evaluators));
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [ScreeningDecisionFormComponent],
      providers: [
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: SupervisorApiService, useValue: supervisorApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
    fixture = TestBed.createComponent(ScreeningDecisionFormComponent);
  }

  it('loads and renders the idea under review', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
    expect(supervisorApi.getUsersByRole).toHaveBeenCalledWith('evaluator');
  });

  it('requires a reason before allowing reject', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    fixture.componentInstance.form.get('decisionCode')!.setValue('reject');
    fixture.componentInstance.form.get('reason')!.setValue('');

    expect(fixture.componentInstance.form.invalid).toBe(true);
  });

  it('blocks submit and shows a validation message when approving with zero evaluators selected', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    fixture.componentInstance.form.get('decisionCode')!.setValue('approve');
    fixture.componentInstance.form.get('reason')!.setValue('');

    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.evaluatorSelectionError()).toContain('evaluator');
    expect(supervisorApi.submitScreeningDecision).not.toHaveBeenCalled();
  });

  it('submits approve with the selected evaluatorIds and navigates to the queue', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    supervisorApi.submitScreeningDecision.and.returnValue(Promise.resolve({ id: 'idea-1', status: 'evaluation' }));
    fixture.componentInstance.form.get('decisionCode')!.setValue('approve');
    fixture.componentInstance.selectedEvaluatorIds.set(['eval-1', 'eval-2']);

    await fixture.componentInstance.onSubmit();

    expect(supervisorApi.submitScreeningDecision).toHaveBeenCalledWith('idea-1', {
      decisionCode: 'approve',
      reason: null,
      evaluatorIds: ['eval-1', 'eval-2'],
    });
    expect(router.navigate).toHaveBeenCalledWith(['/supervisor/screening']);
  });

  it('submits return with the selected editableSections and the required reason', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    supervisorApi.submitScreeningDecision.and.returnValue(Promise.resolve({ id: 'idea-1', status: 'returned' }));
    fixture.componentInstance.form.get('decisionCode')!.setValue('return');
    fixture.componentInstance.form.get('reason')!.setValue('Please add more detail here.');
    fixture.componentInstance.selectedSections.set(['title', 'attachments']);

    expect(fixture.componentInstance.form.invalid).toBe(false);
    await fixture.componentInstance.onSubmit();

    expect(supervisorApi.submitScreeningDecision).toHaveBeenCalledWith('idea-1', {
      decisionCode: 'return',
      reason: 'Please add more detail here.',
      editableSections: ['title', 'attachments'],
    });
    expect(router.navigate).toHaveBeenCalledWith(['/supervisor/screening']);
  });

  it('shows an error state with retry when the idea fails to load, and recovers on retry', async () => {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getById']);
    ideasApi.getById.and.returnValue(Promise.reject({ error: { error: 'Idea not found' } }));
    supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['submitScreeningDecision', 'getUsersByRole']);
    supervisorApi.getUsersByRole.and.returnValue(Promise.resolve(evaluators));
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [ScreeningDecisionFormComponent],
      providers: [
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: SupervisorApiService, useValue: supervisorApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
    fixture = TestBed.createComponent(ScreeningDecisionFormComponent);
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

  it('shows an inline error message when submission fails', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    supervisorApi.submitScreeningDecision.and.returnValue(Promise.reject({ error: { error: 'A reason is required for this decision.' } }));
    fixture.componentInstance.form.get('decisionCode')!.setValue('reject');
    fixture.componentInstance.form.get('reason')!.setValue('A sufficiently long reason.');

    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('A reason is required for this decision.');
  });
});

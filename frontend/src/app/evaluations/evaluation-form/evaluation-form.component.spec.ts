import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { EvaluationsApiService } from '../evaluations-api.service';
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

  function setup(idea: Idea = baseIdea): void {
    evaluationsApi = jasmine.createSpyObj('EvaluationsApiService', ['submit']);
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getById', 'getAttachmentBlob']);
    themesApi = jasmine.createSpyObj('StrategicThemesService', ['list']);
    activitiesApi = jasmine.createSpyObj('ActivitiesService', ['list']);
    challengesApi = jasmine.createSpyObj('ChallengesService', ['listByTheme']);
    router = jasmine.createSpyObj('Router', ['navigate']);

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
    fixture = TestBed.createComponent(EvaluationFormComponent);
    fixture.detectChanges();
  }

  async function setupAndLoad(idea: Idea = baseIdea): Promise<void> {
    setup(idea);
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();
  }

  it('marks the form invalid when a required score field is empty', () => {
    setup();
    fixture.componentInstance.form.patchValue({ innovation: null });

    expect(fixture.componentInstance.form.invalid).toBe(true);
  });

  it('submits all 5 scores and comments, then navigates to the queue on success', async () => {
    setup();
    evaluationsApi.submit.and.returnValue(Promise.resolve({ id: 'eval-1', totalScore: 7, recommendation: 'pass', ideaStatus: 'pass_awaiting_attachments' }));

    fixture.componentInstance.form.setValue({ innovation: 7, impact: 7, execution: 7, scalability: 7, presentation: 7, comments: 'Good idea.' });
    await fixture.componentInstance.onSubmit();

    expect(evaluationsApi.submit).toHaveBeenCalledWith('idea-1', { innovation: 7, impact: 7, execution: 7, scalability: 7, presentation: 7, comments: 'Good idea.' });
    expect(router.navigate).toHaveBeenCalledWith(['/evaluations/queue']);
  });

  it('shows an inline error message when submission fails', async () => {
    setup();
    evaluationsApi.submit.and.returnValue(Promise.reject({ error: { error: 'You have already evaluated this idea.' } }));
    fixture.componentInstance.form.setValue({ innovation: 7, impact: 7, execution: 7, scalability: 7, presentation: 7, comments: null });

    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('You have already evaluated this idea.');
  });

  it('renders each of the 5 criteria as a <select> with 11 numeric options (0..10)', () => {
    setup();

    const selects = ['innovation', 'impact', 'execution', 'scalability', 'presentation'].map(
      (name) => fixture.nativeElement.querySelector(`select[formControlName="${name}"]`) as HTMLSelectElement,
    );

    for (const select of selects) {
      expect(select).withContext('expected a <select> for each criterion').toBeTruthy();
      // 11 numeric options (0..10) plus the leading placeholder option.
      expect(select.querySelectorAll('option').length).toBe(12);
    }
  });

  it('selecting a score option sets a numeric (not string) form value', () => {
    setup();

    const select = fixture.nativeElement.querySelector('select[formControlName="innovation"]') as HTMLSelectElement;
    select.value = select.querySelectorAll('option')[9].value; // the option for 8 (index 0 = placeholder, 1 = 0, ..., 9 = 8)
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.componentInstance.form.controls.innovation.value).toBe(8);
    expect(typeof fixture.componentInstance.form.controls.innovation.value).toBe('number');
  });

  it("shows the idea's title, problem, solution, benefits, classification and attachments beside the form", async () => {
    await setupAndLoad({
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
    await setupAndLoad(baseIdea);

    const panel = fixture.nativeElement.querySelector('app-idea-context-panel');
    expect(panel).toBeTruthy();
  });

  it('does NOT render team members / team roster on the evaluation screen', async () => {
    await setupAndLoad(baseIdea);

    const text = fixture.nativeElement.textContent as string;
    expect(text).not.toContain('Omar Farouk');
    expect(text).not.toContain('ofarouk');
    expect(text).not.toContain('Team A');
  });

  it('opens an attachment via getAttachmentBlob and window.open, mirroring idea-detail', async () => {
    await setupAndLoad({
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
    setup();
    ideasApi.getById.and.returnValue(Promise.reject(new Error('network error')));

    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.ideaInfoUnavailable()).toBe(true);
    expect(fixture.nativeElement.querySelector('form')).toBeTruthy();
    expect(fixture.nativeElement.querySelectorAll('select[formControlName="innovation"]').length).toBe(1);
  });
});

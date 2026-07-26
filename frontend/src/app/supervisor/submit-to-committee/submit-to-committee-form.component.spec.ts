import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { IdeasApiService } from '../../ideas/ideas-api.service';
import { Idea, IdeaAttachment } from '../../ideas/idea.model';
import { SupervisorApiService } from '../supervisor-api.service';
import { RoleUser } from '../supervisor.model';
import { CommitteeApiService } from '../../committee/committee-api.service';
import { SubmitToCommitteeFormComponent } from './submit-to-committee-form.component';

describe('SubmitToCommitteeFormComponent', () => {
  let fixture: ComponentFixture<SubmitToCommitteeFormComponent>;
  let ideasApi: jasmine.SpyObj<IdeasApiService>;
  let supervisorApi: jasmine.SpyObj<SupervisorApiService>;
  let committeeApi: jasmine.SpyObj<CommitteeApiService>;
  let router: jasmine.SpyObj<Router>;

  const baseIdea: Idea = {
    id: 'idea-1', code: 'IDEA-0001', submitterId: 'user-1', titleAr: 'ا', titleEn: 'Title',
    problemStatementAr: 'م', problemStatementEn: 'Problem', proposedSolutionAr: 'ح', proposedSolutionEn: 'Solution',
    expectedBenefitsAr: 'ف', expectedBenefitsEn: 'Benefits', strategicThemeId: 'theme-1',
    activityId: 'activity-1', challengeId: null, participationType: 'individual', teamName: null, teamMembers: [],
    ipAcknowledged: true, termsAgreed: true,
    status: 'pending_final_ranking', currentStage: 3, createdAt: '2026-01-01', updatedAt: '2026-01-01', approvedAt: null, attachments: [] as IdeaAttachment[], screeningReason: null, editableSections: null,
  };

  const judges: RoleUser[] = [
    { id: 'judge-1', fullNameAr: 'خالد', fullNameEn: 'Khalid' },
    { id: 'judge-2', fullNameAr: 'منى', fullNameEn: 'Mona' },
  ];

  function setup(): void {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getById']);
    ideasApi.getById.and.returnValue(Promise.resolve(baseIdea));
    supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['getUsersByRole']);
    supervisorApi.getUsersByRole.and.returnValue(Promise.resolve(judges));
    committeeApi = jasmine.createSpyObj('CommitteeApiService', ['submitToCommittee']);
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [SubmitToCommitteeFormComponent],
      providers: [
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: SupervisorApiService, useValue: supervisorApi },
        { provide: CommitteeApiService, useValue: committeeApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
    fixture = TestBed.createComponent(SubmitToCommitteeFormComponent);
  }

  it('loads the idea and the judge candidates', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
    expect(supervisorApi.getUsersByRole).toHaveBeenCalledWith('judge');
    expect(fixture.nativeElement.textContent).toContain('خالد');
  });

  it('blocks submit and shows a validation message when zero judges are selected', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.judgeSelectionError()).toContain('judge');
    expect(committeeApi.submitToCommittee).not.toHaveBeenCalled();
  });

  it('submits the selected judgeIds and navigates to the committee-pending queue', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    committeeApi.submitToCommittee.and.returnValue(Promise.resolve({ id: 'idea-1', status: 'committee' }));
    fixture.componentInstance.selectedJudgeIds.set(['judge-1', 'judge-2']);

    await fixture.componentInstance.onSubmit();

    expect(committeeApi.submitToCommittee).toHaveBeenCalledWith('idea-1', ['judge-1', 'judge-2']);
    expect(router.navigate).toHaveBeenCalledWith(['/supervisor/committee-pending']);
  });

  it('shows an inline error message when submission fails', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    committeeApi.submitToCommittee.and.returnValue(Promise.reject({ error: { error: 'This idea is not ready for committee.' } }));
    fixture.componentInstance.selectedJudgeIds.set(['judge-1']);

    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('This idea is not ready for committee.');
  });

  it('shows an error state with retry when loading fails, and recovers on retry', async () => {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getById']);
    ideasApi.getById.and.returnValue(Promise.reject({ error: { error: 'Idea not found' } }));
    supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['getUsersByRole']);
    supervisorApi.getUsersByRole.and.returnValue(Promise.resolve(judges));
    committeeApi = jasmine.createSpyObj('CommitteeApiService', ['submitToCommittee']);
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [SubmitToCommitteeFormComponent],
      providers: [
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: SupervisorApiService, useValue: supervisorApi },
        { provide: CommitteeApiService, useValue: committeeApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
    fixture = TestBed.createComponent(SubmitToCommitteeFormComponent);
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

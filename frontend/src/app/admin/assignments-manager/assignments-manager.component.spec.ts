import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AssignmentApiService } from '../assignment-api.service';
import { Assignment, AssignmentPage, IdeaOption } from '../assignment.model';
import { SupervisorApiService } from '../../supervisor/supervisor-api.service';
import { RoleUser } from '../../supervisor/supervisor.model';
import { AssignmentsManagerComponent } from './assignments-manager.component';

describe('AssignmentsManagerComponent', () => {
  let fixture: ComponentFixture<AssignmentsManagerComponent>;
  let api: jasmine.SpyObj<AssignmentApiService>;
  let supervisorApi: jasmine.SpyObj<SupervisorApiService>;

  const evaluators: RoleUser[] = [{ id: 'e-1', fullNameAr: 'م', fullNameEn: 'Evaluator One' }];
  const ideaOptions: IdeaOption[] = [{ id: 'i-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'Idea One' }];
  const page: AssignmentPage = {
    items: [{ id: 'a-1', ideaId: 'i-1', ideaCode: 'IDEA-0001', ideaTitleAr: 'ا', ideaTitleEn: 'Idea One', evaluatorId: 'e-1', evaluatorName: 'Evaluator One', assignedAt: '2026-07-20T00:00:00Z', dueAt: null, statusCode: 'pending', notes: null }],
    total: 1,
    page: 1,
    pageSize: 25,
  };

  function setup(): void {
    api = jasmine.createSpyObj('AssignmentApiService', ['list', 'listIdeaOptions', 'create', 'update', 'unassign', 'bulkUnassign']);
    api.list.and.returnValue(Promise.resolve(page));
    api.listIdeaOptions.and.returnValue(Promise.resolve(ideaOptions));
    supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['getUsersByRole']);
    supervisorApi.getUsersByRole.and.returnValue(Promise.resolve(evaluators));

    TestBed.configureTestingModule({
      imports: [AssignmentsManagerComponent],
      providers: [
        { provide: AssignmentApiService, useValue: api },
        { provide: SupervisorApiService, useValue: supervisorApi },
      ],
    });
    fixture = TestBed.createComponent(AssignmentsManagerComponent);
  }

  it('loads and renders the current page of assignments', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Evaluator One');
    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
  });

  it('reassigns an evaluator and refreshes the list', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    api.update.and.returnValue(Promise.resolve({ ...page.items[0], evaluatorId: 'e-2' }));
    await fixture.componentInstance.onReassign('a-1', 'e-2');

    expect(api.update).toHaveBeenCalledWith('a-1', { statusCode: 'pending', dueAt: null, notes: null, evaluatorId: 'e-2' });
    expect(api.list).toHaveBeenCalledTimes(2);
  });

  it('unassigns a single row and refreshes the list', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    api.unassign.and.returnValue(Promise.resolve());
    await fixture.componentInstance.onUnassign('a-1');

    expect(api.unassign).toHaveBeenCalledWith('a-1');
    expect(api.list).toHaveBeenCalledTimes(2);
  });

  it('bulk-unassigns selected rows and refreshes the list', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    fixture.componentInstance.toggleSelected('a-1');
    api.bulkUnassign.and.returnValue(Promise.resolve({ unassigned: 1 }));
    await fixture.componentInstance.onBulkUnassign();

    expect(api.bulkUnassign).toHaveBeenCalledWith(['a-1']);
    expect(api.list).toHaveBeenCalledTimes(2);
  });

  it('creates a new assignment and refreshes the list', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    api.create.and.returnValue(Promise.resolve({ id: 'a-2' }));
    fixture.componentInstance.updateCreateDraft({ ideaId: 'i-1', evaluatorId: 'e-1' });
    await fixture.componentInstance.onCreate();

    expect(api.create).toHaveBeenCalledWith({ ideaId: 'i-1', evaluatorId: 'e-1', dueAt: null, notes: null });
    expect(api.list).toHaveBeenCalledTimes(2);
  });

  it('shows an empty-state message when there are no assignments', async () => {
    setup();
    api.list.and.returnValue(Promise.resolve({ items: [], total: 0, page: 1, pageSize: 25 }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No assignments match these filters.');
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    api = jasmine.createSpyObj('AssignmentApiService', ['list', 'listIdeaOptions', 'create', 'update', 'unassign', 'bulkUnassign']);
    api.listIdeaOptions.and.returnValue(Promise.resolve(ideaOptions));
    api.list.and.returnValue(Promise.reject({ error: { error: 'Assignments unavailable' } }));
    supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['getUsersByRole']);
    supervisorApi.getUsersByRole.and.returnValue(Promise.resolve(evaluators));

    TestBed.configureTestingModule({
      imports: [AssignmentsManagerComponent],
      providers: [
        { provide: AssignmentApiService, useValue: api },
        { provide: SupervisorApiService, useValue: supervisorApi },
      ],
    });
    fixture = TestBed.createComponent(AssignmentsManagerComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Assignments unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    api.list.and.returnValue(Promise.resolve(page));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Evaluator One');
  });

  it('applyExternalFilter sets the evaluator/status filters and reloads', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    await fixture.componentInstance.applyExternalFilter('e-9', 'pending');

    expect(fixture.componentInstance.filterEvaluatorId()).toBe('e-9');
    expect(fixture.componentInstance.filterStatus()).toBe('pending');
    expect(api.list).toHaveBeenCalledTimes(2);
  });
});

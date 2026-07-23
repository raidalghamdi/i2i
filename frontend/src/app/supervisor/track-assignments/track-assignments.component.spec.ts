import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StrategicThemesService } from '../../ideas/strategic-themes.service';
import { StrategicTheme } from '../../ideas/idea.model';
import { SupervisorApiService } from '../supervisor-api.service';
import { RoleUser, TrackAssignment } from '../supervisor.model';
import { TrackAssignmentsComponent } from './track-assignments.component';

describe('TrackAssignmentsComponent', () => {
  let fixture: ComponentFixture<TrackAssignmentsComponent>;
  let supervisorApi: jasmine.SpyObj<SupervisorApiService>;
  let themesApi: jasmine.SpyObj<StrategicThemesService>;

  function setup(assignments: TrackAssignment[], evaluators: RoleUser[], themes: StrategicTheme[]): void {
    supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['getTrackAssignments', 'createTrackAssignment', 'removeTrackAssignment', 'getUsersByRole']);
    supervisorApi.getTrackAssignments.and.returnValue(Promise.resolve(assignments));
    supervisorApi.getUsersByRole.and.returnValue(Promise.resolve(evaluators));
    themesApi = jasmine.createSpyObj('StrategicThemesService', ['list']);
    themesApi.list.and.returnValue(Promise.resolve(themes));

    TestBed.configureTestingModule({
      imports: [TrackAssignmentsComponent],
      providers: [
        { provide: SupervisorApiService, useValue: supervisorApi },
        { provide: StrategicThemesService, useValue: themesApi },
      ],
    });
    fixture = TestBed.createComponent(TrackAssignmentsComponent);
  }

  it('renders existing assignments, evaluators, and tracks', async () => {
    setup(
      [{ id: 'assign-1', evaluatorId: 'eval-1', evaluatorName: 'Evaluator One', trackId: 'theme-1', trackNameEn: 'Track One' }],
      [{ id: 'eval-1', fullNameAr: 'أ', fullNameEn: 'Evaluator One' }],
      [{ id: 'theme-1', nameAr: 'ا', nameEn: 'Track One' }],
    );
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Evaluator One');
    expect(fixture.nativeElement.textContent).toContain('Track One');
  });

  it('creates a new assignment and refreshes the list', async () => {
    setup([], [{ id: 'eval-1', fullNameAr: 'أ', fullNameEn: 'Evaluator One' }], [{ id: 'theme-1', nameAr: 'ا', nameEn: 'Track One' }]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    supervisorApi.createTrackAssignment.and.returnValue(Promise.resolve({ id: 'assign-1' }));
    supervisorApi.getTrackAssignments.and.returnValue(Promise.resolve([{ id: 'assign-1', evaluatorId: 'eval-1', evaluatorName: 'Evaluator One', trackId: 'theme-1', trackNameEn: 'Track One' }]));

    fixture.componentInstance.selectedEvaluatorId.set('eval-1');
    fixture.componentInstance.selectedTrackId.set('theme-1');
    await fixture.componentInstance.onAssign();

    expect(supervisorApi.createTrackAssignment).toHaveBeenCalledWith({ evaluatorId: 'eval-1', trackId: 'theme-1' });
    expect(fixture.componentInstance.assignments().length).toBe(1);
  });

  it('shows an empty-state message when there are no track assignments', async () => {
    setup([], [{ id: 'eval-1', fullNameAr: 'أ', fullNameEn: 'Evaluator One' }], [{ id: 'theme-1', nameAr: 'ا', nameEn: 'Track One' }]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No track assignments yet.');
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['getTrackAssignments', 'createTrackAssignment', 'removeTrackAssignment', 'getUsersByRole']);
    supervisorApi.getTrackAssignments.and.returnValue(Promise.reject({ error: { error: 'Track assignments unavailable' } }));
    supervisorApi.getUsersByRole.and.returnValue(Promise.resolve([]));
    themesApi = jasmine.createSpyObj('StrategicThemesService', ['list']);
    themesApi.list.and.returnValue(Promise.resolve([]));

    TestBed.configureTestingModule({
      imports: [TrackAssignmentsComponent],
      providers: [
        { provide: SupervisorApiService, useValue: supervisorApi },
        { provide: StrategicThemesService, useValue: themesApi },
      ],
    });
    fixture = TestBed.createComponent(TrackAssignmentsComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Track assignments unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    supervisorApi.getTrackAssignments.and.returnValue(
      Promise.resolve([{ id: 'assign-1', evaluatorId: 'eval-1', evaluatorName: 'Evaluator One', trackId: 'theme-1', trackNameEn: 'Track One' }]),
    );
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Evaluator One');
  });

  it('removes an assignment and refreshes the list', async () => {
    setup(
      [{ id: 'assign-1', evaluatorId: 'eval-1', evaluatorName: 'Evaluator One', trackId: 'theme-1', trackNameEn: 'Track One' }],
      [{ id: 'eval-1', fullNameAr: 'أ', fullNameEn: 'Evaluator One' }],
      [{ id: 'theme-1', nameAr: 'ا', nameEn: 'Track One' }],
    );
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    supervisorApi.removeTrackAssignment.and.returnValue(Promise.resolve());
    supervisorApi.getTrackAssignments.and.returnValue(Promise.resolve([]));

    await fixture.componentInstance.onRemove('assign-1');

    expect(supervisorApi.removeTrackAssignment).toHaveBeenCalledWith('assign-1');
    expect(fixture.componentInstance.assignments().length).toBe(0);
  });
});

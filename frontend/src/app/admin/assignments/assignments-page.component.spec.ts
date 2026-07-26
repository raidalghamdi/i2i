import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { AssignmentApiService } from '../assignment-api.service';
import { SupervisorApiService } from '../../supervisor/supervisor-api.service';
import { AssignmentsPageComponent } from './assignments-page.component';
import { AssignmentsManagerComponent } from '../assignments-manager/assignments-manager.component';

describe('AssignmentsPageComponent', () => {
  let fixture: ComponentFixture<AssignmentsPageComponent>;

  beforeEach(() => {
    const api = jasmine.createSpyObj('AssignmentApiService', ['list', 'getWorkloadHeatmap', 'suggestEvaluators', 'listIdeaOptions']);
    api.list.and.returnValue(Promise.resolve({ items: [], total: 0, page: 1, pageSize: 25 }));
    api.getWorkloadHeatmap.and.returnValue(Promise.resolve([]));
    api.listIdeaOptions.and.returnValue(Promise.resolve([]));
    const supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['getUsersByRole']);
    supervisorApi.getUsersByRole.and.returnValue(Promise.resolve([]));

    TestBed.configureTestingModule({
      imports: [AssignmentsPageComponent],
      providers: [
        { provide: AssignmentApiService, useValue: api },
        { provide: SupervisorApiService, useValue: supervisorApi },
      ],
    });
    fixture = TestBed.createComponent(AssignmentsPageComponent);
  });

  it('renders the page header and all three child sections', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-workload-heatmap')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-evaluator-auto-suggest')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-assignments-manager')).not.toBeNull();
  });

  it('wires heatmap cell clicks to the manager applyExternalFilter method', () => {
    fixture.detectChanges();

    const manager = fixture.debugElement.query(By.directive(AssignmentsManagerComponent))
      .componentInstance as AssignmentsManagerComponent;
    spyOn(manager, 'applyExternalFilter');

    fixture.componentInstance.onHeatmapCellClicked({ evaluatorId: 'e-9', status: 'pending' });

    expect(manager.applyExternalFilter).toHaveBeenCalledWith('e-9', 'pending');
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PhaseScheduleApiService } from '../phase-schedule-api.service';
import { StrategicThemesService } from '../../ideas/strategic-themes.service';
import { AdminPhasesComponent } from './admin-phases.component';

describe('AdminPhasesComponent', () => {
  let fixture: ComponentFixture<AdminPhasesComponent>;

  beforeEach(() => {
    const phaseApi = jasmine.createSpyObj('PhaseScheduleApiService', ['list', 'update']);
    phaseApi.list.and.returnValue(Promise.resolve([]));
    const themesApi = jasmine.createSpyObj('StrategicThemesService', ['list', 'create', 'update', 'delete']);
    themesApi.list.and.returnValue(Promise.resolve([]));

    TestBed.configureTestingModule({
      imports: [AdminPhasesComponent],
      providers: [
        { provide: PhaseScheduleApiService, useValue: phaseApi },
        { provide: StrategicThemesService, useValue: themesApi },
      ],
    });
    fixture = TestBed.createComponent(AdminPhasesComponent);
  });

  it('renders the page header and both child sections', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-phase-schedule-editor')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-tracks-manager')).not.toBeNull();
  });
});

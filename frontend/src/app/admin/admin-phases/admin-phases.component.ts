import { Component } from '@angular/core';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { PhaseScheduleEditorComponent } from '../phase-schedule-editor/phase-schedule-editor.component';
import { TracksManagerComponent } from '../tracks-manager/tracks-manager.component';

@Component({
  selector: 'app-admin-phases',
  imports: [PageHeaderComponent, PhaseScheduleEditorComponent, TracksManagerComponent],
  templateUrl: './admin-phases.component.html',
})
export class AdminPhasesComponent {}

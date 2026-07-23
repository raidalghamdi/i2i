import { Component, ViewChild } from '@angular/core';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { WorkloadHeatmapComponent } from '../workload-heatmap/workload-heatmap.component';
import { EvaluatorAutoSuggestComponent } from '../evaluator-auto-suggest/evaluator-auto-suggest.component';
import { AssignmentsManagerComponent } from '../assignments-manager/assignments-manager.component';

@Component({
  selector: 'app-assignments-page',
  imports: [PageHeaderComponent, WorkloadHeatmapComponent, EvaluatorAutoSuggestComponent, AssignmentsManagerComponent],
  templateUrl: './assignments-page.component.html',
})
export class AssignmentsPageComponent {
  @ViewChild(AssignmentsManagerComponent) private manager?: AssignmentsManagerComponent;

  onHeatmapCellClicked(event: { evaluatorId: string; status: string }): void {
    this.manager?.applyExternalFilter(event.evaluatorId, event.status);
  }
}

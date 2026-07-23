import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AssignmentApiService } from '../assignment-api.service';
import { WorkloadRow } from '../assignment.model';
import { WorkloadHeatmapComponent } from './workload-heatmap.component';

describe('WorkloadHeatmapComponent', () => {
  let fixture: ComponentFixture<WorkloadHeatmapComponent>;
  let api: jasmine.SpyObj<AssignmentApiService>;

  const rows: WorkloadRow[] = [
    { evaluatorId: 'e-1', evaluatorName: 'Evaluator One', pending: 2, dueSoon: 1, overdue: 0, completedRecent: 3 },
  ];

  function setup(): void {
    api = jasmine.createSpyObj('AssignmentApiService', ['getWorkloadHeatmap']);
    api.getWorkloadHeatmap.and.returnValue(Promise.resolve(rows));

    TestBed.configureTestingModule({
      imports: [WorkloadHeatmapComponent],
      providers: [{ provide: AssignmentApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(WorkloadHeatmapComponent);
  }

  it('loads and renders one row per evaluator', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Evaluator One');
  });

  it('emits cellClicked with the evaluator id and bucket status when a cell is clicked', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    let emitted: { evaluatorId: string; status: string } | undefined;
    fixture.componentInstance.cellClicked.subscribe((e) => (emitted = e));

    fixture.componentInstance.onCellClick('e-1', 'pending');

    expect(emitted).toEqual({ evaluatorId: 'e-1', status: 'pending' });
  });

  it('shows an error state with retry when the heatmap call fails, and recovers on retry', async () => {
    api = jasmine.createSpyObj('AssignmentApiService', ['getWorkloadHeatmap']);
    api.getWorkloadHeatmap.and.returnValue(Promise.reject(new Error('boom')));
    TestBed.configureTestingModule({
      imports: [WorkloadHeatmapComponent],
      providers: [{ provide: AssignmentApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(WorkloadHeatmapComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeTruthy();
    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    expect(retryButton).toBeTruthy();

    api.getWorkloadHeatmap.and.returnValue(Promise.resolve(rows));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Evaluator One');
  });

  it('shows an empty-state message when there are no rows', async () => {
    api = jasmine.createSpyObj('AssignmentApiService', ['getWorkloadHeatmap']);
    api.getWorkloadHeatmap.and.returnValue(Promise.resolve([]));
    TestBed.configureTestingModule({
      imports: [WorkloadHeatmapComponent],
      providers: [{ provide: AssignmentApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(WorkloadHeatmapComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No workload data yet');
  });
});

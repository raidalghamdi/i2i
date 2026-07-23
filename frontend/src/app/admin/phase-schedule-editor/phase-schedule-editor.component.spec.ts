import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PhaseScheduleApiService } from '../phase-schedule-api.service';
import { PhaseSchedule } from '../phase-schedule.model';
import { PhaseScheduleEditorComponent } from './phase-schedule-editor.component';

describe('PhaseScheduleEditorComponent', () => {
  let fixture: ComponentFixture<PhaseScheduleEditorComponent>;
  let api: jasmine.SpyObj<PhaseScheduleApiService>;

  function setup(phases: PhaseSchedule[]): void {
    api = jasmine.createSpyObj('PhaseScheduleApiService', ['list', 'update']);
    api.list.and.returnValue(Promise.resolve(phases));

    TestBed.configureTestingModule({
      imports: [PhaseScheduleEditorComponent],
      providers: [{ provide: PhaseScheduleApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(PhaseScheduleEditorComponent);
  }

  const phase: PhaseSchedule = { idx: 0, code: 'submission', labelAr: 'تقديم الأفكار', labelEn: 'Idea Submission', startsAt: null, endsAt: null, updatedAt: '2026-07-20T00:00:00Z' };

  it('renders all seeded phases', async () => {
    setup([phase]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Idea Submission');
  });

  it('shows an empty-state message when there are no phases', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No phases configured yet.');
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    api = jasmine.createSpyObj('PhaseScheduleApiService', ['list', 'update']);
    api.list.and.returnValue(Promise.reject({ error: { error: 'Phase schedules unavailable' } }));

    TestBed.configureTestingModule({
      imports: [PhaseScheduleEditorComponent],
      providers: [{ provide: PhaseScheduleApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(PhaseScheduleEditorComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Phase schedules unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    api.list.and.returnValue(Promise.resolve([phase]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Idea Submission');
  });

  it('reports unscheduled status when no dates are set', () => {
    setup([phase]);
    expect(fixture.componentInstance.status(phase)).toBe('unscheduled');
  });

  it('reports active status when now is within the window', () => {
    setup([phase]);
    const active: PhaseSchedule = { ...phase, startsAt: new Date(Date.now() - 1000).toISOString(), endsAt: new Date(Date.now() + 1000 * 60 * 60).toISOString() };
    expect(fixture.componentInstance.status(active)).toBe('active');
  });

  it('saves a phase and updates the list with the server response', async () => {
    setup([phase]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    const updated: PhaseSchedule = { ...phase, startsAt: '2026-08-01T00:00:00Z', endsAt: '2026-08-31T00:00:00Z' };
    api.update.and.returnValue(Promise.resolve(updated));

    await fixture.componentInstance.save(0);

    expect(api.update).toHaveBeenCalledWith(0, { startsAt: null, endsAt: null });
    expect(fixture.componentInstance.phases()[0].startsAt).toBe('2026-08-01T00:00:00Z');
  });
});

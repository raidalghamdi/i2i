import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PhaseScheduleApiService } from '../phase-schedule-api.service';
import { PhaseSchedule } from '../phase-schedule.model';
import { RosterApiService } from '../roster-api.service';
import { RosterHubRow } from '../roster.model';
import { PhaseScheduleEditorComponent } from './phase-schedule-editor.component';

describe('PhaseScheduleEditorComponent', () => {
  let fixture: ComponentFixture<PhaseScheduleEditorComponent>;
  let api: jasmine.SpyObj<PhaseScheduleApiService>;
  let rosterApi: jasmine.SpyObj<RosterApiService>;

  const roles: RosterHubRow[] = [
    { roleCode: 'judge', roleNameAr: 'محكم', roleNameEn: 'Judge', activeCount: 1, pendingCount: 0, expiredCount: 0, withdrawnCount: 0 },
    { roleCode: 'supervisor', roleNameAr: 'مشرف', roleNameEn: 'Supervisor', activeCount: 1, pendingCount: 0, expiredCount: 0, withdrawnCount: 0 },
    { roleCode: 'evaluator', roleNameAr: 'مقيم', roleNameEn: 'Evaluator', activeCount: 1, pendingCount: 0, expiredCount: 0, withdrawnCount: 0 },
  ];

  function setup(phases: PhaseSchedule[], audienceRoleCodes: string[] = []): void {
    api = jasmine.createSpyObj('PhaseScheduleApiService', ['list', 'update', 'getAudience', 'setAudience', 'announce']);
    api.list.and.returnValue(Promise.resolve(phases));
    api.getAudience.and.returnValue(Promise.resolve({ roleCodes: audienceRoleCodes }));
    api.setAudience.and.returnValue(Promise.resolve({ roleCodes: audienceRoleCodes }));
    api.announce.and.returnValue(Promise.resolve({ recipientCount: 0 }));

    rosterApi = jasmine.createSpyObj('RosterApiService', ['getHub']);
    rosterApi.getHub.and.returnValue(Promise.resolve(roles));

    TestBed.configureTestingModule({
      imports: [PhaseScheduleEditorComponent],
      providers: [
        { provide: PhaseScheduleApiService, useValue: api },
        { provide: RosterApiService, useValue: rosterApi },
      ],
    });
    fixture = TestBed.createComponent(PhaseScheduleEditorComponent);
  }

  const phase: PhaseSchedule = {
    idx: 0,
    code: 'submission',
    labelAr: 'تقديم الأفكار',
    labelEn: 'Idea Submission',
    startsAt: null,
    endsAt: null,
    updatedAt: '2026-07-20T00:00:00Z',
    announcedAt: null,
  };

  it('renders all seeded phases', async () => {
    setup([phase]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('تقديم الأفكار');
  });

  it('shows an empty-state message when there are no phases', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No phases configured yet.');
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    api = jasmine.createSpyObj('PhaseScheduleApiService', ['list', 'update', 'getAudience', 'setAudience', 'announce']);
    api.list.and.returnValue(Promise.reject({ error: { error: 'Phase schedules unavailable' } }));
    api.getAudience.and.returnValue(Promise.resolve({ roleCodes: [] }));
    api.setAudience.and.returnValue(Promise.resolve({ roleCodes: [] }));
    api.announce.and.returnValue(Promise.resolve({ recipientCount: 0 }));

    rosterApi = jasmine.createSpyObj('RosterApiService', ['getHub']);
    rosterApi.getHub.and.returnValue(Promise.resolve(roles));

    TestBed.configureTestingModule({
      imports: [PhaseScheduleEditorComponent],
      providers: [
        { provide: PhaseScheduleApiService, useValue: api },
        { provide: RosterApiService, useValue: rosterApi },
      ],
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
    expect(fixture.nativeElement.textContent).toContain('تقديم الأفكار');
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

  it("loads each phase's audience roles and renders the role picker with checked roles", async () => {
    setup([phase], ['judge']);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(rosterApi.getHub).toHaveBeenCalled();
    expect(api.getAudience).toHaveBeenCalledWith(0);
    expect(fixture.nativeElement.textContent).toContain('محكم');

    const checkboxes = fixture.nativeElement.querySelectorAll('input[type="checkbox"]') as NodeListOf<HTMLInputElement>;
    const judgeCheckbox = Array.from(checkboxes).find((c) => c.value === 'judge');
    expect(judgeCheckbox?.checked).toBe(true);
    const supervisorCheckbox = Array.from(checkboxes).find((c) => c.value === 'supervisor');
    expect(supervisorCheckbox?.checked).toBe(false);
  });

  it('saving the audience PUTs the selected role codes', async () => {
    setup([phase], ['judge']);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    fixture.componentInstance.toggleAudienceRole(0, 'supervisor', true);
    await fixture.componentInstance.saveAudience(0);

    expect(api.setAudience).toHaveBeenCalledWith(0, jasmine.arrayContaining(['judge', 'supervisor']));
  });

  it('announcing (after confirmation) calls announce and displays the recipient count', async () => {
    setup([phase]);
    spyOn(window, 'confirm').and.returnValue(true);
    api.announce.and.returnValue(Promise.resolve({ recipientCount: 4 }));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const announceButton = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find((b) =>
      b.textContent?.includes('Announce'),
    );
    expect(announceButton).toBeTruthy();

    announceButton!.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(window.confirm).toHaveBeenCalled();
    expect(api.announce).toHaveBeenCalledWith(0);
    expect(fixture.nativeElement.textContent).toContain('4');
  });

  it('does not announce when the confirmation is dismissed', async () => {
    setup([phase]);
    spyOn(window, 'confirm').and.returnValue(false);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const announceButton = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find((b) =>
      b.textContent?.includes('Announce'),
    );
    announceButton!.click();
    await fixture.whenStable();

    expect(api.announce).not.toHaveBeenCalled();
  });
});

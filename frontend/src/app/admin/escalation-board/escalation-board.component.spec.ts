import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Escalation } from '../escalations.model';
import { EscalationsApiService } from '../escalations-api.service';
import { EscalationBoardComponent } from './escalation-board.component';

describe('EscalationBoardComponent', () => {
  let fixture: ComponentFixture<EscalationBoardComponent>;
  let escalationsApi: jasmine.SpyObj<EscalationsApiService>;

  const sampleEscalation: Escalation = {
    id: 'e1',
    entityType: 'evaluation',
    entityId: 'idea-1',
    tierCode: 'manager',
    tierNameEn: 'Manager',
    reasonAr: 'أ',
    reasonEn: 'SLA breach: evaluation exceeded target of 72h',
    statusCode: 'open',
    statusNameEn: 'Open',
    ownerName: 'Manager One',
    openedAt: '2026-01-01T00:00:00Z',
  };

  function setup(escalations: Escalation[]): void {
    escalationsApi = jasmine.createSpyObj('EscalationsApiService', ['list', 'acknowledge', 'bump', 'resolve']);
    escalationsApi.list.and.returnValue(Promise.resolve(escalations));

    TestBed.configureTestingModule({
      imports: [EscalationBoardComponent],
      providers: [provideRouter([]), { provide: EscalationsApiService, useValue: escalationsApi }],
    });
    fixture = TestBed.createComponent(EscalationBoardComponent);
  }

  it('renders one row per escalation', async () => {
    setup([sampleEscalation]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('SLA breach: evaluation exceeded target of 72h');
    expect(fixture.nativeElement.textContent).toContain('Manager One');
  });

  it('shows an empty-state message when there are no escalations', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No escalations');
  });

  it('re-lists with the selected filters when a filter changes', async () => {
    setup([sampleEscalation]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    escalationsApi.list.calls.reset();
    fixture.componentInstance.statusFilter.set('resolved');
    await fixture.componentInstance.onFilterChange();

    expect(escalationsApi.list).toHaveBeenCalledWith({ status: 'resolved', tier: undefined, entityType: undefined });
  });

  it('acknowledges an escalation and refreshes the list', async () => {
    setup([sampleEscalation]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const acknowledged = { ...sampleEscalation, statusCode: 'acknowledged', statusNameEn: 'Acknowledged' };
    escalationsApi.acknowledge.and.returnValue(Promise.resolve(acknowledged));
    escalationsApi.list.and.returnValue(Promise.resolve([acknowledged]));

    await fixture.componentInstance.onAcknowledge('e1');

    expect(escalationsApi.acknowledge).toHaveBeenCalledWith('e1', { notes: null });
    expect(fixture.componentInstance.escalations()[0].statusCode).toBe('acknowledged');
  });

  it('bumps an escalation and refreshes the list', async () => {
    setup([sampleEscalation]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const bumped = { ...sampleEscalation, tierCode: 'director', tierNameEn: 'Director' };
    escalationsApi.bump.and.returnValue(Promise.resolve(bumped));
    escalationsApi.list.and.returnValue(Promise.resolve([bumped]));

    await fixture.componentInstance.onBump('e1');

    expect(escalationsApi.bump).toHaveBeenCalledWith('e1', { notes: null });
    expect(fixture.componentInstance.escalations()[0].tierCode).toBe('director');
  });

  it('resolves an escalation with entered resolution text and refreshes the list', async () => {
    setup([sampleEscalation]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    escalationsApi.resolve.and.returnValue(Promise.resolve({ ...sampleEscalation, statusCode: 'resolved' }));
    escalationsApi.list.and.returnValue(Promise.resolve([]));

    fixture.componentInstance.resolutionAr.set('تم');
    fixture.componentInstance.resolutionEn.set('fixed');
    await fixture.componentInstance.onResolve('e1');

    expect(escalationsApi.resolve).toHaveBeenCalledWith('e1', { resolutionAr: 'تم', resolutionEn: 'fixed' });
    expect(fixture.componentInstance.escalations().length).toBe(0);
  });

  it('shows an error state with retry when the list call fails, and recovers on retry', async () => {
    escalationsApi = jasmine.createSpyObj('EscalationsApiService', ['list', 'acknowledge', 'bump', 'resolve']);
    escalationsApi.list.and.returnValue(Promise.reject({ error: { error: 'boom' } }));
    TestBed.configureTestingModule({
      imports: [EscalationBoardComponent],
      providers: [provideRouter([]), { provide: EscalationsApiService, useValue: escalationsApi }],
    });
    fixture = TestBed.createComponent(EscalationBoardComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('boom');
    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    expect(retryButton).toBeTruthy();

    escalationsApi.list.and.returnValue(Promise.resolve([sampleEscalation]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('SLA breach');
  });
});

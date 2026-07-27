import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SlaPoliciesApiService } from '../sla-policies-api.service';
import { SlaPolicy } from '../sla-policies.model';
import { SlaPoliciesComponent } from './sla-policies.component';

// Change 20260726
describe('SlaPoliciesComponent', () => {
  let fixture: ComponentFixture<SlaPoliciesComponent>;
  let api: jasmine.SpyObj<SlaPoliciesApiService>;

  const policyA: SlaPolicy = {
    id: 'sla-1',
    entityType: 'idea',
    fromState: 'submitted',
    toState: 'under_evaluation',
    targetHours: 48,
    warnAtPct: 75,
  };

  const policyB: SlaPolicy = {
    id: 'sla-2',
    entityType: 'idea',
    fromState: 'under_evaluation',
    toState: 'evaluation_review',
    targetHours: 72,
    warnAtPct: 80,
  };

  async function setup(policies: SlaPolicy[]): Promise<void> {
    api = jasmine.createSpyObj('SlaPoliciesApiService', ['list', 'create', 'update', 'remove']);
    api.list.and.returnValue(Promise.resolve(policies));
    api.create.and.returnValue(Promise.resolve(policies[0]));
    api.update.and.returnValue(Promise.resolve(policies[0]));
    api.remove.and.returnValue(Promise.resolve());

    await TestBed.configureTestingModule({
      imports: [SlaPoliciesComponent],
      providers: [provideRouter([]), { provide: SlaPoliciesApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(SlaPoliciesComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('renders one row per policy', async () => {
    await setup([policyA, policyB]);

    const fromInputs = Array.from(
      fixture.nativeElement.querySelectorAll('tbody tr td:nth-child(2) input'),
    ) as HTMLInputElement[];
    expect(fromInputs.map((input) => input.value)).toEqual(['submitted', 'under_evaluation']);
  });

  it('shows an empty-state message when there are no policies', async () => {
    await setup([]);

    expect(fixture.nativeElement.textContent).toContain('No SLA policies');
  });

  it('saves an edited existing row via update() and reloads', async () => {
    await setup([policyA, policyB]);

    const row = fixture.componentInstance.editableRows()[0];
    fixture.componentInstance.updateRow(row.localKey, { targetHours: 24 });

    const updated = { ...policyA, targetHours: 24 };
    api.update.and.returnValue(Promise.resolve(updated));
    api.list.and.returnValue(Promise.resolve([updated, policyB]));

    await fixture.componentInstance.onSave(fixture.componentInstance.editableRows()[0]);

    expect(api.update).toHaveBeenCalledWith('sla-1', {
      entityType: 'idea',
      fromState: 'submitted',
      toState: 'under_evaluation',
      targetHours: 24,
      warnAtPct: 75,
    });
    expect(api.list).toHaveBeenCalledTimes(2);
  });

  it('adds a blank row with sensible defaults and saves it via create()', async () => {
    await setup([policyA]);

    fixture.componentInstance.onAddRow();
    const newRow = fixture.componentInstance.editableRows().at(-1)!;
    expect(newRow.id).toBeNull();
    expect(newRow.targetHours).toBe(24);
    expect(newRow.warnAtPct).toBe(80);

    fixture.componentInstance.updateRow(newRow.localKey, {
      entityType: 'escalation',
      fromState: 'open',
      toState: 'resolved',
    });

    const created: SlaPolicy = {
      id: 'sla-3',
      entityType: 'escalation',
      fromState: 'open',
      toState: 'resolved',
      targetHours: 24,
      warnAtPct: 80,
    };
    api.create.and.returnValue(Promise.resolve(created));
    api.list.and.returnValue(Promise.resolve([policyA, created]));

    await fixture.componentInstance.onSave(fixture.componentInstance.editableRows().at(-1)!);

    expect(api.create).toHaveBeenCalledWith({
      entityType: 'escalation',
      fromState: 'open',
      toState: 'resolved',
      targetHours: 24,
      warnAtPct: 80,
    });
  });

  it('deletes a row via remove() and reloads', async () => {
    await setup([policyA, policyB]);

    api.list.and.returnValue(Promise.resolve([policyB]));

    await fixture.componentInstance.onDelete('sla-1');

    expect(api.remove).toHaveBeenCalledWith('sla-1');
    expect(fixture.componentInstance.rows().length).toBe(1);
  });

  it('surfaces the duplicate-transition error when save fails', async () => {
    await setup([policyA, policyB]);

    api.update.and.returnValue(
      Promise.reject({ error: { error: 'An SLA policy for this transition already exists.' } }),
    );

    await fixture.componentInstance.onSave(fixture.componentInstance.editableRows()[0]);

    expect(fixture.componentInstance.errorMessage()).toBe(
      'An SLA policy for this transition already exists.',
    );
  });

  it('shows an error state with retry when the list call fails, and recovers on retry', async () => {
    api = jasmine.createSpyObj('SlaPoliciesApiService', ['list', 'create', 'update', 'remove']);
    api.list.and.returnValue(Promise.reject({ error: { error: 'boom' } }));

    await TestBed.configureTestingModule({
      imports: [SlaPoliciesComponent],
      providers: [provideRouter([]), { provide: SlaPoliciesApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(SlaPoliciesComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('boom');
    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    expect(retryButton).toBeTruthy();

    api.list.and.returnValue(Promise.resolve([policyA]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.componentInstance.editableRows().length).toBe(1);
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EvaluationCriteriaApiService } from '../evaluation-criteria-api.service';
import { EvaluationCriterion } from '../evaluation-criteria.model';
import { EvaluationCriteriaComponent } from './evaluation-criteria.component';

// Change 20260726
describe('EvaluationCriteriaComponent', () => {
  let fixture: ComponentFixture<EvaluationCriteriaComponent>;
  let api: jasmine.SpyObj<EvaluationCriteriaApiService>;

  const criterionA: EvaluationCriterion = {
    id: 'ecrit-1',
    code: 'impact',
    nameAr: 'الأثر',
    nameEn: 'Impact',
    descriptionAr: null,
    descriptionEn: null,
    weight: 0.6,
    active: true,
    sortOrder: 1,
  };

  const criterionB: EvaluationCriterion = {
    id: 'ecrit-2',
    code: 'feasibility',
    nameAr: 'الجدوى',
    nameEn: 'Feasibility',
    descriptionAr: null,
    descriptionEn: null,
    weight: 0.4,
    active: true,
    sortOrder: 2,
  };

  async function setup(criteria: EvaluationCriterion[]): Promise<void> {
    api = jasmine.createSpyObj('EvaluationCriteriaApiService', ['list', 'create', 'update', 'remove']);
    api.list.and.returnValue(Promise.resolve(criteria));
    api.create.and.returnValue(Promise.resolve(criteria[0]));
    api.update.and.returnValue(Promise.resolve(criteria[0]));
    api.remove.and.returnValue(Promise.resolve());

    await TestBed.configureTestingModule({
      imports: [EvaluationCriteriaComponent],
      providers: [provideRouter([]), { provide: EvaluationCriteriaApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(EvaluationCriteriaComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('renders one row per criterion', async () => {
    await setup([criterionA, criterionB]);

    const codeInputs = Array.from(
      fixture.nativeElement.querySelectorAll('tbody tr td:first-child input'),
    ) as HTMLInputElement[];
    expect(codeInputs.map((input) => input.value)).toEqual(['impact', 'feasibility']);
  });

  it('shows an empty-state message when there are no criteria', async () => {
    await setup([]);

    expect(fixture.nativeElement.textContent).toContain('No evaluation criteria');
  });

  it('does not show a weight-warning banner when active weights sum to 1.0', async () => {
    await setup([criterionA, criterionB]);

    expect(fixture.nativeElement.textContent).not.toContain('should sum to 1.0');
  });

  it('shows a weight-warning banner when active weights do not sum to 1.0', async () => {
    await setup([criterionA, criterionB]);

    fixture.componentInstance.updateRow(fixture.componentInstance.editableRows()[0].localKey, { weight: 0.9 });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('should sum to 1.0');
  });

  it('saves an edited existing row via update() and reloads', async () => {
    await setup([criterionA, criterionB]);

    const row = fixture.componentInstance.editableRows()[0];
    fixture.componentInstance.updateRow(row.localKey, { nameEn: 'Impact (revised)' });

    const updated = { ...criterionA, nameEn: 'Impact (revised)' };
    api.update.and.returnValue(Promise.resolve(updated));
    api.list.and.returnValue(Promise.resolve([updated, criterionB]));

    await fixture.componentInstance.onSave(fixture.componentInstance.editableRows()[0]);

    expect(api.update).toHaveBeenCalledWith('ecrit-1', {
      code: 'impact',
      nameAr: 'الأثر',
      nameEn: 'Impact (revised)',
      descriptionAr: null,
      descriptionEn: null,
      weight: 0.6,
      active: true,
      sortOrder: 1,
    });
    expect(api.list).toHaveBeenCalledTimes(2);
  });

  it('adds a blank row and saves it via create()', async () => {
    await setup([criterionA, criterionB]);

    fixture.componentInstance.onAddRow();
    fixture.detectChanges();
    const newRow = fixture.componentInstance.editableRows().at(-1)!;
    expect(newRow.id).toBeNull();

    fixture.componentInstance.updateRow(newRow.localKey, {
      code: 'novelty',
      nameAr: 'الابتكار',
      nameEn: 'Novelty',
      weight: 0.1,
    });

    const created: EvaluationCriterion = {
      id: 'ecrit-3',
      code: 'novelty',
      nameAr: 'الابتكار',
      nameEn: 'Novelty',
      descriptionAr: null,
      descriptionEn: null,
      weight: 0.1,
      active: true,
      sortOrder: 3,
    };
    api.create.and.returnValue(Promise.resolve(created));
    api.list.and.returnValue(Promise.resolve([criterionA, criterionB, created]));

    await fixture.componentInstance.onSave(fixture.componentInstance.editableRows().at(-1)!);

    expect(api.create).toHaveBeenCalledWith({
      code: 'novelty',
      nameAr: 'الابتكار',
      nameEn: 'Novelty',
      descriptionAr: null,
      descriptionEn: null,
      weight: 0.1,
      active: true,
      sortOrder: 3,
    });
  });

  it('deletes a row via remove() and reloads', async () => {
    await setup([criterionA, criterionB]);

    api.remove.and.returnValue(Promise.resolve());
    api.list.and.returnValue(Promise.resolve([criterionB]));

    await fixture.componentInstance.onDelete('ecrit-1');

    expect(api.remove).toHaveBeenCalledWith('ecrit-1');
    expect(fixture.componentInstance.rows().length).toBe(1);
  });

  it('surfaces an error message when save fails', async () => {
    await setup([criterionA, criterionB]);

    api.update.and.returnValue(Promise.reject({ error: { error: 'An evaluation criterion with this code already exists.' } }));

    await fixture.componentInstance.onSave(fixture.componentInstance.editableRows()[0]);

    expect(fixture.componentInstance.errorMessage()).toBe(
      'An evaluation criterion with this code already exists.',
    );
  });

  it('shows an error state with retry when the list call fails, and recovers on retry', async () => {
    api = jasmine.createSpyObj('EvaluationCriteriaApiService', ['list', 'create', 'update', 'remove']);
    api.list.and.returnValue(Promise.reject({ error: { error: 'boom' } }));

    await TestBed.configureTestingModule({
      imports: [EvaluationCriteriaComponent],
      providers: [provideRouter([]), { provide: EvaluationCriteriaApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(EvaluationCriteriaComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('boom');
    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    expect(retryButton).toBeTruthy();

    api.list.and.returnValue(Promise.resolve([criterionA]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    const codeInput: HTMLInputElement = fixture.nativeElement.querySelector('tbody tr td:first-child input');
    expect(codeInput.value).toBe('impact');
  });
});

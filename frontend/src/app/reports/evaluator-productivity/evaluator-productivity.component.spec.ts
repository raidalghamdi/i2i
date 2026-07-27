import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EvaluatorProductivityApiService } from '../evaluator-productivity-api.service';
import { EvaluatorProductivityRow } from '../evaluator-productivity.model';
import { EvaluatorProductivityComponent } from './evaluator-productivity.component';

// Change 20260726
describe('EvaluatorProductivityComponent', () => {
  let fixture: ComponentFixture<EvaluatorProductivityComponent>;
  let api: jasmine.SpyObj<EvaluatorProductivityApiService>;

  const amal: EvaluatorProductivityRow = {
    userId: 'u-1',
    displayName: 'Amal',
    assignedCount: 10,
    completedCount: 4,
    draftCount: 2,
    avgScore: 72.5,
    avgTurnaroundHours: 30,
    coiCount: 1,
  };

  const badr: EvaluatorProductivityRow = {
    userId: 'u-2',
    displayName: 'Badr',
    assignedCount: 3,
    completedCount: 9,
    draftCount: 0,
    avgScore: 61,
    avgTurnaroundHours: 12,
    coiCount: 0,
  };

  const zaid: EvaluatorProductivityRow = {
    userId: 'u-3',
    displayName: 'Zaid',
    assignedCount: 5,
    completedCount: 0,
    draftCount: 1,
    avgScore: null,
    avgTurnaroundHours: null,
    coiCount: 3,
  };

  function names(): string[] {
    return Array.from(fixture.nativeElement.querySelectorAll('tbody tr td:first-child')).map(
      (cell) => (cell as HTMLElement).textContent!.trim(),
    );
  }

  async function setup(rows: EvaluatorProductivityRow[]): Promise<void> {
    api = jasmine.createSpyObj('EvaluatorProductivityApiService', ['list']);
    api.list.and.returnValue(Promise.resolve(rows));

    await TestBed.configureTestingModule({
      imports: [EvaluatorProductivityComponent],
      providers: [provideRouter([]), { provide: EvaluatorProductivityApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(EvaluatorProductivityComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('defaults to sorting by completed count descending', async () => {
    await setup([amal, badr, zaid]);

    expect(fixture.componentInstance.sortKey()).toBe('completedCount');
    expect(names()).toEqual(['Badr', 'Amal', 'Zaid']);
  });

  it('shows an empty-state message when no evaluators are returned', async () => {
    await setup([]);

    expect(fixture.nativeElement.textContent).toContain('No evaluator activity');
  });

  it('sorts by a numeric column descending on first click', async () => {
    await setup([amal, badr, zaid]);

    fixture.nativeElement.querySelector('[data-testid="sort-assignedCount"]').click();
    fixture.detectChanges();

    expect(names()).toEqual(['Amal', 'Zaid', 'Badr']);
  });

  it('toggles direction when the active column is clicked again', async () => {
    await setup([amal, badr, zaid]);

    fixture.nativeElement.querySelector('[data-testid="sort-completedCount"]').click();
    fixture.detectChanges();

    expect(fixture.componentInstance.sortDescending()).toBeFalse();
    expect(names()).toEqual(['Zaid', 'Amal', 'Badr']);
  });

  it('sorts the evaluator name column ascending on first click', async () => {
    await setup([badr, zaid, amal]);

    fixture.nativeElement.querySelector('[data-testid="sort-displayName"]').click();
    fixture.detectChanges();

    expect(fixture.componentInstance.sortDescending()).toBeFalse();
    expect(names()).toEqual(['Amal', 'Badr', 'Zaid']);
  });

  it('keeps rows without an average last in both sort directions', async () => {
    await setup([amal, badr, zaid]);

    fixture.nativeElement.querySelector('[data-testid="sort-avgScore"]').click();
    fixture.detectChanges();
    expect(names()).toEqual(['Amal', 'Badr', 'Zaid']);

    fixture.nativeElement.querySelector('[data-testid="sort-avgScore"]').click();
    fixture.detectChanges();
    expect(names()).toEqual(['Badr', 'Amal', 'Zaid']);
  });

  it('renders a dash instead of a zero for missing averages', async () => {
    await setup([zaid]);

    const cells = Array.from(fixture.nativeElement.querySelectorAll('tbody tr td')) as HTMLElement[];
    expect(cells[4].textContent!.trim()).toBe('—');
    expect(cells[5].textContent!.trim()).toBe('—');
  });

  it('exposes the active sort direction to assistive technology', async () => {
    await setup([amal, badr]);

    const headers = Array.from(fixture.nativeElement.querySelectorAll('thead th')) as HTMLElement[];
    expect(headers[2].getAttribute('aria-sort')).toBe('descending');
    expect(headers[1].getAttribute('aria-sort')).toBe('none');
  });

  it('shows an error state with retry when the fetch fails, and recovers on retry', async () => {
    api = jasmine.createSpyObj('EvaluatorProductivityApiService', ['list']);
    api.list.and.returnValue(Promise.reject({ error: { error: 'boom' } }));

    await TestBed.configureTestingModule({
      imports: [EvaluatorProductivityComponent],
      providers: [provideRouter([]), { provide: EvaluatorProductivityApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(EvaluatorProductivityComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('boom');
    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    expect(retryButton).toBeTruthy();

    api.list.and.returnValue(Promise.resolve([amal]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(names()).toEqual(['Amal']);
  });
});

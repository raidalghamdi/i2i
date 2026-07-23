import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AssignmentApiService } from '../assignment-api.service';
import { SuggestedEvaluator } from '../assignment.model';
import { EvaluatorAutoSuggestComponent } from './evaluator-auto-suggest.component';

describe('EvaluatorAutoSuggestComponent', () => {
  let fixture: ComponentFixture<EvaluatorAutoSuggestComponent>;
  let api: jasmine.SpyObj<AssignmentApiService>;

  const suggestions: SuggestedEvaluator[] = [
    { evaluatorId: 'e-1', evaluatorName: 'Aaron Idle', openCount: 0 },
    { evaluatorId: 'e-2', evaluatorName: 'Busy Evaluator', openCount: 3 },
  ];

  function setup(): void {
    api = jasmine.createSpyObj('AssignmentApiService', ['suggestEvaluators']);
    api.suggestEvaluators.and.returnValue(Promise.resolve(suggestions));

    TestBed.configureTestingModule({
      imports: [EvaluatorAutoSuggestComponent],
      providers: [{ provide: AssignmentApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(EvaluatorAutoSuggestComponent);
  }

  it('does not fetch suggestions until the button is clicked', () => {
    setup();
    fixture.detectChanges();

    expect(api.suggestEvaluators).not.toHaveBeenCalled();
    expect(fixture.componentInstance.suggestions().length).toBe(0);
  });

  it('fetches and displays suggestions when the button is clicked', async () => {
    setup();
    fixture.detectChanges();

    await fixture.componentInstance.onSuggestClick();
    fixture.detectChanges();

    expect(fixture.componentInstance.suggestions().length).toBe(2);
    expect(fixture.nativeElement.textContent).toContain('Aaron Idle');
    expect(fixture.nativeElement.textContent).toContain('Busy Evaluator');
  });

  it('shows an empty-state message when the search returns no evaluators', async () => {
    setup();
    api.suggestEvaluators.and.returnValue(Promise.resolve([]));
    fixture.detectChanges();

    await fixture.componentInstance.onSuggestClick();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No available evaluators found');
  });

  it('shows an error state with retry when suggesting fails, and recovers on retry', async () => {
    setup();
    api.suggestEvaluators.and.returnValue(Promise.reject(new Error('boom')));
    fixture.detectChanges();

    await fixture.componentInstance.onSuggestClick();
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBeTruthy();
    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    expect(retryButton).toBeTruthy();

    api.suggestEvaluators.and.returnValue(Promise.resolve(suggestions));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Aaron Idle');
  });
});

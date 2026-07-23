import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { EvaluationsApiService } from '../evaluations-api.service';
import { EvaluationFormComponent } from './evaluation-form.component';

describe('EvaluationFormComponent', () => {
  let fixture: ComponentFixture<EvaluationFormComponent>;
  let evaluationsApi: jasmine.SpyObj<EvaluationsApiService>;
  let router: jasmine.SpyObj<Router>;

  function setup(): void {
    evaluationsApi = jasmine.createSpyObj('EvaluationsApiService', ['submit']);
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [EvaluationFormComponent],
      providers: [
        { provide: EvaluationsApiService, useValue: evaluationsApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
    fixture = TestBed.createComponent(EvaluationFormComponent);
    fixture.detectChanges();
  }

  it('marks the form invalid when a required score field is empty', () => {
    setup();
    fixture.componentInstance.form.patchValue({ innovation: null });

    expect(fixture.componentInstance.form.invalid).toBe(true);
  });

  it('submits all 5 scores and comments, then navigates to the queue on success', async () => {
    setup();
    evaluationsApi.submit.and.returnValue(Promise.resolve({ id: 'eval-1', totalScore: 7, recommendation: 'pass', ideaStatus: 'pass_awaiting_attachments' }));

    fixture.componentInstance.form.setValue({ innovation: 7, impact: 7, execution: 7, scalability: 7, presentation: 7, comments: 'Good idea.' });
    await fixture.componentInstance.onSubmit();

    expect(evaluationsApi.submit).toHaveBeenCalledWith('idea-1', { innovation: 7, impact: 7, execution: 7, scalability: 7, presentation: 7, comments: 'Good idea.' });
    expect(router.navigate).toHaveBeenCalledWith(['/evaluations/queue']);
  });

  it('shows an inline error message when submission fails', async () => {
    setup();
    evaluationsApi.submit.and.returnValue(Promise.reject({ error: { error: 'You have already evaluated this idea.' } }));
    fixture.componentInstance.form.setValue({ innovation: 7, impact: 7, execution: 7, scalability: 7, presentation: 7, comments: null });

    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('You have already evaluated this idea.');
  });
});

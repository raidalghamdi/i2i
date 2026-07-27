import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EvaluationsApiService } from '../evaluations-api.service';
import { MyEvaluation } from '../evaluation.model';
import { MyEvaluationsListComponent } from './my-evaluations-list.component';

describe('MyEvaluationsListComponent', () => {
  let fixture: ComponentFixture<MyEvaluationsListComponent>;
  let evaluationsApi: jasmine.SpyObj<EvaluationsApiService>;

  function setup(evaluations: MyEvaluation[]): void {
    evaluationsApi = jasmine.createSpyObj('EvaluationsApiService', ['getMine']);
    evaluationsApi.getMine.and.returnValue(Promise.resolve(evaluations));

    TestBed.configureTestingModule({
      imports: [MyEvaluationsListComponent],
      providers: [provideRouter([]), { provide: EvaluationsApiService, useValue: evaluationsApi }],
    });
    fixture = TestBed.createComponent(MyEvaluationsListComponent);
  }

  it('renders one row per evaluation with score and recommendation', async () => {
    setup([
      { id: 'eval-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'One', totalScore: 7, recommendation: 'pass', submittedAt: '2026-01-01', ideaEnteredEvaluationAt: null },
    ]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
    expect(fixture.nativeElement.textContent).toContain('7');
    expect(fixture.nativeElement.textContent).toContain('pass');
  });

  // Change 20260726
  it('offers a resume link pointing at the idea for an unsubmitted draft', async () => {
    setup([
      { id: 'eval-1', ideaId: 'idea-7', ideaCode: 'IDEA-0007', ideaTitleEn: 'Draft one', totalScore: 0, recommendation: 'pending', submittedAt: null, ideaEnteredEvaluationAt: null },
    ]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('tbody a') as HTMLAnchorElement;
    expect(link).withContext('expected a resume-draft link').toBeTruthy();
    expect(link.textContent).toContain('Resume draft');
    expect(link.getAttribute('href')).toBe('/evaluations/idea-7');
    // A draft has no score or recommendation to show yet.
    expect(fixture.nativeElement.textContent).not.toContain('pending');
  });

  // Change 20260726
  it('shows a submitted row without a resume link', async () => {
    setup([
      { id: 'eval-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'One', totalScore: 7, recommendation: 'pass', submittedAt: '2026-01-01', ideaEnteredEvaluationAt: null },
    ]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('tbody a')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Submitted');
  });

  it('shows an empty-state message when there are no evaluations', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain("haven't submitted");
  });

  it('shows the error state and retries the fetch when "Try again" is clicked', async () => {
    setup([]);
    evaluationsApi.getMine.and.returnValue(Promise.reject(new Error('boom')));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).not.toBeNull();
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();

    const evaluations: MyEvaluation[] = [
      { id: 'eval-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'One', totalScore: 7, recommendation: 'pass', submittedAt: '2026-01-01', ideaEnteredEvaluationAt: null },
    ];
    evaluationsApi.getMine.and.returnValue(Promise.resolve(evaluations));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBeNull();
    expect(fixture.componentInstance.evaluations().length).toBe(1);
  });
});

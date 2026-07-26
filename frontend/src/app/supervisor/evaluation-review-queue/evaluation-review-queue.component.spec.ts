import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EvaluationReviewApiService } from '../../evaluations/evaluation-review-api.service';
import { SupervisorQueueItem } from '../supervisor.model';
import { EvaluationReviewQueueComponent } from './evaluation-review-queue.component';

describe('EvaluationReviewQueueComponent', () => {
  let fixture: ComponentFixture<EvaluationReviewQueueComponent>;
  let evaluationReviewApi: jasmine.SpyObj<EvaluationReviewApiService>;

  function setup(queue: SupervisorQueueItem[]): void {
    evaluationReviewApi = jasmine.createSpyObj('EvaluationReviewApiService', ['getReviewQueue']);
    evaluationReviewApi.getReviewQueue.and.returnValue(Promise.resolve(queue));

    TestBed.configureTestingModule({
      imports: [EvaluationReviewQueueComponent],
      providers: [provideRouter([]), { provide: EvaluationReviewApiService, useValue: evaluationReviewApi }],
    });
    fixture = TestBed.createComponent(EvaluationReviewQueueComponent);
  }

  it('renders one row per queued idea, including submitter name and aggregate score', async () => {
    setup([
      { id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'One', submitterName: 'Submitter One', strategicThemeId: 'theme-1', evaluationAggregateScore: 7.5, updatedAt: '2026-01-01' },
    ]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('li').length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Submitter One');
    expect(fixture.nativeElement.textContent).toContain('7.5');
  });

  it('shows a placeholder when the aggregate score is not yet available', async () => {
    setup([
      { id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'One', submitterName: 'Submitter One', strategicThemeId: 'theme-1', evaluationAggregateScore: null, updatedAt: '2026-01-01' },
    ]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No score yet');
  });

  it('shows an empty-state message when the queue is empty', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('no ideas awaiting evaluation review');
  });

  it('shows an error state with retry when the queue fails to load, and recovers on retry', async () => {
    evaluationReviewApi = jasmine.createSpyObj('EvaluationReviewApiService', ['getReviewQueue']);
    evaluationReviewApi.getReviewQueue.and.returnValue(Promise.reject({ error: { error: 'Queue unavailable' } }));

    TestBed.configureTestingModule({
      imports: [EvaluationReviewQueueComponent],
      providers: [provideRouter([]), { provide: EvaluationReviewApiService, useValue: evaluationReviewApi }],
    });
    fixture = TestBed.createComponent(EvaluationReviewQueueComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBe('Queue unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    evaluationReviewApi.getReviewQueue.and.returnValue(
      Promise.resolve([
        { id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'One', submitterName: 'Submitter One', strategicThemeId: 'theme-1', evaluationAggregateScore: null, updatedAt: '2026-01-01' },
      ]),
    );
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBeNull();
    expect(fixture.componentInstance.queue().length).toBe(1);
  });
});

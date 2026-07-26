import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EvaluationReviewApiService } from '../../evaluations/evaluation-review-api.service';
import { SupervisorQueueItem } from '../supervisor.model';
import { CommitteePendingQueueComponent } from './committee-pending-queue.component';

describe('CommitteePendingQueueComponent', () => {
  let fixture: ComponentFixture<CommitteePendingQueueComponent>;
  let evaluationReviewApi: jasmine.SpyObj<EvaluationReviewApiService>;

  function setup(queue: SupervisorQueueItem[]): void {
    evaluationReviewApi = jasmine.createSpyObj('EvaluationReviewApiService', ['getCommitteePendingQueue']);
    evaluationReviewApi.getCommitteePendingQueue.and.returnValue(Promise.resolve(queue));

    TestBed.configureTestingModule({
      imports: [CommitteePendingQueueComponent],
      providers: [provideRouter([]), { provide: EvaluationReviewApiService, useValue: evaluationReviewApi }],
    });
    fixture = TestBed.createComponent(CommitteePendingQueueComponent);
  }

  it('renders one row per queued idea, linking to the submit-to-committee screen', async () => {
    setup([
      { id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'One', submitterName: 'Submitter One', strategicThemeId: 'theme-1', evaluationAggregateScore: 8.2, updatedAt: '2026-01-01' },
    ]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('li').length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Submitter One');
    const link = fixture.nativeElement.querySelector('a') as HTMLAnchorElement;
    expect(link.getAttribute('href')).toBe('/supervisor/submit-to-committee/idea-1');
  });

  it('shows an empty-state message when the queue is empty', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('no ideas awaiting referral to committee');
  });

  it('shows an error state with retry when the queue fails to load, and recovers on retry', async () => {
    evaluationReviewApi = jasmine.createSpyObj('EvaluationReviewApiService', ['getCommitteePendingQueue']);
    evaluationReviewApi.getCommitteePendingQueue.and.returnValue(Promise.reject({ error: { error: 'Queue unavailable' } }));

    TestBed.configureTestingModule({
      imports: [CommitteePendingQueueComponent],
      providers: [provideRouter([]), { provide: EvaluationReviewApiService, useValue: evaluationReviewApi }],
    });
    fixture = TestBed.createComponent(CommitteePendingQueueComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBe('Queue unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    evaluationReviewApi.getCommitteePendingQueue.and.returnValue(
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

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EvaluationsApiService } from '../evaluations-api.service';
import { EvaluationQueueItem } from '../evaluation.model';
import { EvaluatorQueueComponent } from './evaluator-queue.component';

describe('EvaluatorQueueComponent', () => {
  let fixture: ComponentFixture<EvaluatorQueueComponent>;
  let evaluationsApi: jasmine.SpyObj<EvaluationsApiService>;

  function setup(queue: EvaluationQueueItem[]): void {
    evaluationsApi = jasmine.createSpyObj('EvaluationsApiService', ['getQueue']);
    evaluationsApi.getQueue.and.returnValue(Promise.resolve(queue));

    TestBed.configureTestingModule({
      imports: [EvaluatorQueueComponent],
      providers: [provideRouter([]), { provide: EvaluationsApiService, useValue: evaluationsApi }],
    });
    fixture = TestBed.createComponent(EvaluatorQueueComponent);
  }

  it('renders one row per queued idea, including submitter name', async () => {
    setup([
      { id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'One', submitterName: 'Submitter One', strategicThemeId: 'theme-1', updatedAt: '2026-01-01' },
    ]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('li').length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Submitter One');
  });

  it('shows an empty-state message when the queue is empty', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('no ideas');
  });

  it('shows the error state and retries the fetch when "Try again" is clicked', async () => {
    setup([]);
    evaluationsApi.getQueue.and.returnValue(Promise.reject(new Error('boom')));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).not.toBeNull();
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();

    const queue: EvaluationQueueItem[] = [
      { id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'One', submitterName: 'Submitter One', strategicThemeId: 'theme-1', updatedAt: '2026-01-01' },
    ];
    evaluationsApi.getQueue.and.returnValue(Promise.resolve(queue));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBeNull();
    expect(fixture.componentInstance.queue().length).toBe(1);
  });
});

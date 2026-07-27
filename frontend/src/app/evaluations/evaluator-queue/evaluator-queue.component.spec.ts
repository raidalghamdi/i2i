import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EvaluationsApiService } from '../evaluations-api.service';
import { EvaluationQueueItem } from '../evaluation.model';
import { EvaluatorQueueComponent } from './evaluator-queue.component';
import { OwnIdeasService } from '../../core/own-ideas.service'; // Change 20260726

describe('EvaluatorQueueComponent', () => {
  let fixture: ComponentFixture<EvaluatorQueueComponent>;
  let evaluationsApi: jasmine.SpyObj<EvaluationsApiService>;
  let ownIdeas: jasmine.SpyObj<OwnIdeasService>; // Change 20260726

  function setup(queue: EvaluationQueueItem[], ownIdeaIds: string[] = []): void {
    evaluationsApi = jasmine.createSpyObj('EvaluationsApiService', ['getQueue']);
    evaluationsApi.getQueue.and.returnValue(Promise.resolve(queue));
    // Change 20260726
    ownIdeas = jasmine.createSpyObj('OwnIdeasService', ['loadOwnIdeaIds']);
    ownIdeas.loadOwnIdeaIds.and.returnValue(Promise.resolve(new Set(ownIdeaIds)));

    TestBed.configureTestingModule({
      imports: [EvaluatorQueueComponent],
      providers: [
        provideRouter([]),
        { provide: EvaluationsApiService, useValue: evaluationsApi },
        { provide: OwnIdeasService, useValue: ownIdeas }, // Change 20260726
      ],
    });
    fixture = TestBed.createComponent(EvaluatorQueueComponent);
  }

  it('renders one row per queued idea, without exposing the submitter identity (anonymized queue)', async () => {
    setup([
      { id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'One', strategicThemeId: 'theme-1', updatedAt: '2026-01-01' },
    ]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('li').length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
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
      { id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'One', strategicThemeId: 'theme-1', updatedAt: '2026-01-01' },
    ];
    evaluationsApi.getQueue.and.returnValue(Promise.resolve(queue));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBeNull();
    expect(fixture.componentInstance.queue().length).toBe(1);
  });

  // Change 20260726
  describe('self-authored ideas', () => {
    const own: EvaluationQueueItem = { id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'One', strategicThemeId: 'theme-1', updatedAt: '2026-01-01' };
    const other: EvaluationQueueItem = { id: 'idea-2', code: 'IDEA-0002', titleAr: 'ب', titleEn: 'Two', strategicThemeId: 'theme-1', updatedAt: '2026-01-01' };

    async function render(queue: EvaluationQueueItem[], ownIdeaIds: string[]): Promise<void> {
      setup(queue, ownIdeaIds);
      fixture.detectChanges();
      await fixture.componentInstance.ngOnInit();
      fixture.detectChanges();
    }

    it('does not link to the evaluation form for an idea the evaluator submitted', async () => {
      await render([own, other], ['idea-1']);

      const hrefs = (Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[])
        .map((a) => a.getAttribute('href'));
      expect(hrefs).toContain('/evaluations/idea-2');
      expect(hrefs).not.toContain('/evaluations/idea-1');
    });

    it('marks the own idea with an explanatory tooltip', async () => {
      await render([own], ['idea-1']);

      expect(fixture.componentInstance.isOwnIdea('idea-1')).toBeTrue();
      expect(fixture.nativeElement.textContent).toContain('Your own idea');
      const tooltipped = fixture.nativeElement.querySelector('li [title]') as HTMLElement;
      expect(tooltipped.getAttribute('title')).toBe('You cannot evaluate your own idea');
    });

    it('leaves other submitters\' ideas actionable', async () => {
      await render([other], ['idea-1']);

      expect(fixture.componentInstance.isOwnIdea('idea-2')).toBeFalse();
      expect(fixture.nativeElement.querySelector('a')?.getAttribute('href')).toBe('/evaluations/idea-2');
    });
  });
});

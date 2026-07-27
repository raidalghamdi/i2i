import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CommitteeApiService } from '../committee-api.service';
import { CommitteeQueueItem } from '../committee.model';
import { CommitteeQueueComponent } from './committee-queue.component';
import { OwnIdeasService } from '../../core/own-ideas.service'; // Change 20260726

describe('CommitteeQueueComponent', () => {
  let fixture: ComponentFixture<CommitteeQueueComponent>;
  let committeeApi: jasmine.SpyObj<CommitteeApiService>;
  let ownIdeas: jasmine.SpyObj<OwnIdeasService>; // Change 20260726

  function setup(queue: CommitteeQueueItem[], ownIdeaIds: string[] = []): void {
    committeeApi = jasmine.createSpyObj('CommitteeApiService', ['getQueue']);
    committeeApi.getQueue.and.returnValue(Promise.resolve(queue));
    // Change 20260726
    ownIdeas = jasmine.createSpyObj('OwnIdeasService', ['loadOwnIdeaIds']);
    ownIdeas.loadOwnIdeaIds.and.returnValue(Promise.resolve(new Set(ownIdeaIds)));

    TestBed.configureTestingModule({
      imports: [CommitteeQueueComponent],
      providers: [
        provideRouter([]),
        { provide: CommitteeApiService, useValue: committeeApi },
        { provide: OwnIdeasService, useValue: ownIdeas }, // Change 20260726
      ],
    });
    fixture = TestBed.createComponent(CommitteeQueueComponent);
  }

  it('renders one row per queued idea, including submitter name and decided count', async () => {
    setup([
      { id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'One', submitterName: 'Submitter One', hasDecided: false, decidedCount: 1, totalJudges: 2, updatedAt: '2026-01-01' },
    ]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('li').length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Submitter One');
    expect(fixture.nativeElement.textContent).toContain('1');
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
    committeeApi.getQueue.and.returnValue(Promise.reject(new Error('boom')));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).not.toBeNull();
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();

    const queue: CommitteeQueueItem[] = [
      { id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'One', submitterName: 'Submitter One', hasDecided: false, decidedCount: 1, totalJudges: 2, updatedAt: '2026-01-01' },
    ];
    committeeApi.getQueue.and.returnValue(Promise.resolve(queue));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBeNull();
    expect(fixture.componentInstance.queue().length).toBe(1);
  });

  // Change 20260726
  describe('self-authored ideas', () => {
    const own: CommitteeQueueItem = { id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'One', submitterName: 'Me', hasDecided: false, decidedCount: 0, totalJudges: 2, updatedAt: '2026-01-01' };
    const other: CommitteeQueueItem = { id: 'idea-2', code: 'IDEA-0002', titleAr: 'ب', titleEn: 'Two', submitterName: 'Someone', hasDecided: false, decidedCount: 0, totalJudges: 2, updatedAt: '2026-01-01' };

    async function render(queue: CommitteeQueueItem[], ownIdeaIds: string[]): Promise<void> {
      setup(queue, ownIdeaIds);
      fixture.detectChanges();
      await fixture.componentInstance.ngOnInit();
      fixture.detectChanges();
    }

    it('does not link to the decision form for an idea the judge submitted', async () => {
      await render([own, other], ['idea-1']);

      const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
      const hrefs = links.map((a) => a.getAttribute('href'));
      expect(hrefs).toContain('/committee/idea-2');
      expect(hrefs).not.toContain('/committee/idea-1');
    });

    it('marks the own idea with an explanatory tooltip', async () => {
      await render([own], ['idea-1']);

      expect(fixture.componentInstance.isOwnIdea('idea-1')).toBeTrue();
      expect(fixture.nativeElement.textContent).toContain('Your own idea');
      const tooltipped = fixture.nativeElement.querySelector('li [title]') as HTMLElement;
      expect(tooltipped.getAttribute('title')).toBe('You cannot decide on your own idea');
    });

    it('leaves other judges\' ideas actionable', async () => {
      await render([other], ['idea-1']);

      expect(fixture.componentInstance.isOwnIdea('idea-2')).toBeFalse();
      expect(fixture.nativeElement.querySelector('a')?.getAttribute('href')).toBe('/committee/idea-2');
      expect(fixture.nativeElement.textContent).not.toContain('Your own idea');
    });
  });
});

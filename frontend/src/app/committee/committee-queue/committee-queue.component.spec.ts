import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CommitteeApiService } from '../committee-api.service';
import { CommitteeQueueItem } from '../committee.model';
import { CommitteeQueueComponent } from './committee-queue.component';

describe('CommitteeQueueComponent', () => {
  let fixture: ComponentFixture<CommitteeQueueComponent>;
  let committeeApi: jasmine.SpyObj<CommitteeApiService>;

  function setup(queue: CommitteeQueueItem[]): void {
    committeeApi = jasmine.createSpyObj('CommitteeApiService', ['getQueue']);
    committeeApi.getQueue.and.returnValue(Promise.resolve(queue));

    TestBed.configureTestingModule({
      imports: [CommitteeQueueComponent],
      providers: [provideRouter([]), { provide: CommitteeApiService, useValue: committeeApi }],
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
});

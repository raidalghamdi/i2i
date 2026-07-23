import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SupervisorApiService } from '../supervisor-api.service';
import { ScreeningQueueItem } from '../supervisor.model';
import { ScreeningQueueComponent } from './screening-queue.component';

describe('ScreeningQueueComponent', () => {
  let fixture: ComponentFixture<ScreeningQueueComponent>;
  let supervisorApi: jasmine.SpyObj<SupervisorApiService>;

  function setup(queue: ScreeningQueueItem[]): void {
    supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['getScreeningQueue']);
    supervisorApi.getScreeningQueue.and.returnValue(Promise.resolve(queue));

    TestBed.configureTestingModule({
      imports: [ScreeningQueueComponent],
      providers: [provideRouter([]), { provide: SupervisorApiService, useValue: supervisorApi }],
    });
    fixture = TestBed.createComponent(ScreeningQueueComponent);
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

  it('shows an error state with retry when the queue fails to load, and recovers on retry', async () => {
    supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['getScreeningQueue']);
    supervisorApi.getScreeningQueue.and.returnValue(Promise.reject({ error: { error: 'Screening queue unavailable' } }));

    TestBed.configureTestingModule({
      imports: [ScreeningQueueComponent],
      providers: [provideRouter([]), { provide: SupervisorApiService, useValue: supervisorApi }],
    });
    fixture = TestBed.createComponent(ScreeningQueueComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBe('Screening queue unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    supervisorApi.getScreeningQueue.and.returnValue(
      Promise.resolve([
        { id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'One', submitterName: 'Submitter One', strategicThemeId: 'theme-1', updatedAt: '2026-01-01' },
      ]),
    );
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBeNull();
    expect(fixture.componentInstance.queue().length).toBe(1);
  });
});

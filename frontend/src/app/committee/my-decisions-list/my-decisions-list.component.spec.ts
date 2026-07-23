import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommitteeApiService } from '../committee-api.service';
import { MyCommitteeDecision } from '../committee.model';
import { MyDecisionsListComponent } from './my-decisions-list.component';

describe('MyDecisionsListComponent', () => {
  let fixture: ComponentFixture<MyDecisionsListComponent>;
  let committeeApi: jasmine.SpyObj<CommitteeApiService>;

  function setup(decisions: MyCommitteeDecision[]): void {
    committeeApi = jasmine.createSpyObj('CommitteeApiService', ['getMine']);
    committeeApi.getMine.and.returnValue(Promise.resolve(decisions));

    TestBed.configureTestingModule({
      imports: [MyDecisionsListComponent],
      providers: [{ provide: CommitteeApiService, useValue: committeeApi }],
    });
    fixture = TestBed.createComponent(MyDecisionsListComponent);
  }

  it('renders one row per decision with score', async () => {
    setup([
      { id: 'decision-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'One', totalScore: 8, decidedAt: '2026-01-01' },
    ]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
    expect(fixture.nativeElement.textContent).toContain('8');
  });

  it('shows an empty-state message when there are no decisions', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain("haven't submitted");
  });

  it('shows the error state and retries the fetch when "Try again" is clicked', async () => {
    setup([]);
    committeeApi.getMine.and.returnValue(Promise.reject(new Error('boom')));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).not.toBeNull();
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();

    const decisions: MyCommitteeDecision[] = [
      { id: 'decision-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'One', totalScore: 8, decidedAt: '2026-01-01' },
    ];
    committeeApi.getMine.and.returnValue(Promise.resolve(decisions));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBeNull();
    expect(fixture.componentInstance.decisions().length).toBe(1);
  });
});

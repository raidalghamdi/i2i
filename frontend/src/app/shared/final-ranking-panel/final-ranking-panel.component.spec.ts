import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SupervisorApiService } from '../../supervisor/supervisor-api.service';
import { FinalRankingResult } from '../../supervisor/supervisor.model';
import { FinalRankingPanelComponent } from './final-ranking-panel.component';

describe('FinalRankingPanelComponent', () => {
  let fixture: ComponentFixture<FinalRankingPanelComponent>;
  let supervisorApi: jasmine.SpyObj<SupervisorApiService>;

  const sample: FinalRankingResult = {
    approvedCount: 1,
    notSelectedCount: 1,
    topN: 1,
    entries: [
      { ideaId: 'idea-1', code: 'IDEA-0001', titleEn: 'One', trackId: 't1', rank: 1, score: 9, outcomeStatus: 'approved' },
      { ideaId: 'idea-2', code: 'IDEA-0002', titleEn: 'Two', trackId: 't1', rank: 2, score: 5, outcomeStatus: 'not_selected' },
    ],
  };

  function setup(): void {
    supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['previewFinalRanking', 'runFinalRanking']);
    TestBed.configureTestingModule({
      imports: [FinalRankingPanelComponent],
      providers: [{ provide: SupervisorApiService, useValue: supervisorApi }],
    });
    fixture = TestBed.createComponent(FinalRankingPanelComponent);
  }

  it('previews the ranking and shows entries', async () => {
    setup();
    supervisorApi.previewFinalRanking.and.resolveTo(sample);
    fixture.detectChanges();
    await fixture.componentInstance.onPreview();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
  });

  it('shows an error message when run fails', async () => {
    setup();
    supervisorApi.runFinalRanking.and.rejectWith({ error: { error: 'Cannot run yet.' } });
    fixture.detectChanges();
    await fixture.componentInstance.onRun();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Cannot run yet.');
  });

  it('runs the final ranking and shows the result summary', async () => {
    setup();
    const result: FinalRankingResult = { approvedCount: 3, notSelectedCount: 2, topN: 3, entries: [] };
    supervisorApi.runFinalRanking.and.resolveTo(result);
    fixture.detectChanges();
    await fixture.componentInstance.onRun();
    fixture.detectChanges();
    expect(fixture.componentInstance.result()).toEqual(result);
  });
});

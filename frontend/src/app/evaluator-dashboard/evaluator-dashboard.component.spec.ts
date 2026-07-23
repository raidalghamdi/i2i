import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EvaluationsApiService } from '../evaluations/evaluations-api.service';
import { EvaluationQueueItem, MyEvaluation } from '../evaluations/evaluation.model';
import { MeApiService } from '../core/me-api.service';
import { EvaluatorDashboardComponent } from './evaluator-dashboard.component';

describe('EvaluatorDashboardComponent', () => {
  let fixture: ComponentFixture<EvaluatorDashboardComponent>;
  let evaluationsApi: jasmine.SpyObj<EvaluationsApiService>;
  let meApi: jasmine.SpyObj<MeApiService>;

  const now = new Date();
  const thisMonthIso = new Date(now.getFullYear(), now.getMonth(), 15).toISOString();
  const lastMonthIso = new Date(now.getFullYear(), now.getMonth() - 1, 15).toISOString();

  const queue: EvaluationQueueItem[] = [
    { id: 'i1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'Alpha', submitterName: 'S1', strategicThemeId: 't1', updatedAt: '2026-07-01T00:00:00Z' },
    { id: 'i2', code: 'IDEA-0002', titleAr: 'ب', titleEn: 'Beta', submitterName: 'S2', strategicThemeId: 't1', updatedAt: '2026-07-02T00:00:00Z' },
  ];
  const mine: MyEvaluation[] = [
    { id: 'e1', ideaId: 'i3', ideaCode: 'IDEA-0003', ideaTitleEn: 'Gamma', totalScore: 8, recommendation: 'pass', submittedAt: thisMonthIso, ideaEnteredEvaluationAt: new Date(now.getFullYear(), now.getMonth(), 10).toISOString() },
    { id: 'e2', ideaId: 'i4', ideaCode: 'IDEA-0004', ideaTitleEn: 'Delta', totalScore: 6, recommendation: 'pass', submittedAt: lastMonthIso, ideaEnteredEvaluationAt: null },
  ];

  function setup(): void {
    evaluationsApi = jasmine.createSpyObj('EvaluationsApiService', ['getQueue', 'getMine']);
    meApi = jasmine.createSpyObj('MeApiService', ['get']);

    evaluationsApi.getQueue.and.returnValue(Promise.resolve(queue));
    evaluationsApi.getMine.and.returnValue(Promise.resolve(mine));
    meApi.get.and.returnValue(Promise.resolve({
      id: 'u1', samAccountName: 'e1', email: 'e1@x.com', fullNameAr: 'م', fullNameEn: 'E',
      department: null, title: null, points: 80, level: 2, roles: ['evaluator'],
    }));

    TestBed.configureTestingModule({
      imports: [EvaluatorDashboardComponent],
      providers: [
        provideRouter([]),
        { provide: EvaluationsApiService, useValue: evaluationsApi },
        { provide: MeApiService, useValue: meApi },
      ],
    });
    fixture = TestBed.createComponent(EvaluatorDashboardComponent);
  }

  it('counts the awaiting queue and this-months evaluations', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.awaitingCount()).toBe(2);
    expect(fixture.componentInstance.evaluatedThisMonthCount()).toBe(1);
  });

  it('computes avg days to complete only from rows with a non-null ideaEnteredEvaluationAt', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    // e1: entered on day 10, submitted on day 15 -> 5 days. e2 excluded (null entered date).
    expect(fixture.componentInstance.avgDays()).toBe(5);
  });

  it('reports null avg days when no row has a non-null ideaEnteredEvaluationAt', async () => {
    evaluationsApi = jasmine.createSpyObj('EvaluationsApiService', ['getQueue', 'getMine']);
    meApi = jasmine.createSpyObj('MeApiService', ['get']);
    evaluationsApi.getQueue.and.returnValue(Promise.resolve([]));
    evaluationsApi.getMine.and.returnValue(Promise.resolve([
      { id: 'e1', ideaId: 'i1', ideaCode: 'IDEA-0001', ideaTitleEn: 'A', totalScore: 5, recommendation: 'pass', submittedAt: thisMonthIso, ideaEnteredEvaluationAt: null },
    ]));
    meApi.get.and.returnValue(Promise.resolve({
      id: 'u1', samAccountName: 'e1', email: 'e1@x.com', fullNameAr: 'م', fullNameEn: 'E',
      department: null, title: null, points: 0, level: 1, roles: ['evaluator'],
    }));
    TestBed.configureTestingModule({
      imports: [EvaluatorDashboardComponent],
      providers: [
        provideRouter([]),
        { provide: EvaluationsApiService, useValue: evaluationsApi },
        { provide: MeApiService, useValue: meApi },
      ],
    });
    fixture = TestBed.createComponent(EvaluatorDashboardComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.avgDays()).toBeNull();
  });

  it('shows the top 5 newest-first queue items in the preview', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.queuePreview().length).toBe(2);
    expect(fixture.componentInstance.queuePreview()[0].id).toBe('i2');
  });

  it('loads points and level from MeApiService', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.points()).toBe(80);
    expect(fixture.componentInstance.level()).toBe(2);
  });

  it('renders the error state and retries the fetch when the "Try again" button is clicked', async () => {
    setup();
    evaluationsApi.getQueue.and.returnValue(Promise.reject(new Error('boom')));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).not.toBeNull();
    const retryButton = (fixture.nativeElement as HTMLElement).querySelector('button');
    expect(retryButton).toBeTruthy();

    evaluationsApi.getQueue.and.returnValue(Promise.resolve(queue));
    retryButton!.dispatchEvent(new Event('click'));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBeNull();
    expect(fixture.componentInstance.awaitingCount()).toBe(2);
  });
});

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { EvaluationReviewApiService } from './evaluation-review-api.service';
import { EvaluationReviewDecisionInput } from './evaluation-review.model';

describe('EvaluationReviewApiService', () => {
  let service: EvaluationReviewApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(EvaluationReviewApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getDetail() gets /api/ideas/{id}/evaluation-detail', async () => {
    const promise = service.getDetail('idea-1');
    const req = httpMock.expectOne('/api/ideas/idea-1/evaluation-detail');
    expect(req.request.method).toBe('GET');
    const detail = {
      evaluations: [{ reviewerLabel: 'Reviewer 1', score: 8, comment: 'Solid.' }],
      aggregateScore: 8,
      aggregateByCriteria: { innovation: 8 },
      supervisorComment: null,
    };
    req.flush(detail);

    expect(await promise).toEqual(detail);
  });

  it('submitDecision() posts to /api/ideas/{id}/evaluation-review-decision', async () => {
    const input: EvaluationReviewDecisionInput = { decisionCode: 'forward', supervisorComment: 'Looks good.' };
    const promise = service.submitDecision('idea-1', input);
    const req = httpMock.expectOne('/api/ideas/idea-1/evaluation-review-decision');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(input);
    req.flush({ id: 'idea-1', status: 'pass_awaiting_attachments' });

    expect(await promise).toEqual({ id: 'idea-1', status: 'pass_awaiting_attachments' });
  });

  it('getReviewQueue() gets /api/supervisor/evaluation-review-queue', async () => {
    const item = {
      id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'T', submitterName: 'Submitter One',
      strategicThemeId: 'theme-1', evaluationAggregateScore: 7.5, updatedAt: '2026-01-01',
    };
    const promise = service.getReviewQueue();
    const req = httpMock.expectOne('/api/supervisor/evaluation-review-queue');
    expect(req.request.method).toBe('GET');
    req.flush([item]);

    expect(await promise).toEqual([item]);
  });

  it('getCommitteePendingQueue() gets /api/supervisor/committee-pending-queue', async () => {
    const item = {
      id: 'idea-2', code: 'IDEA-0002', titleAr: 'ب', titleEn: 'U', submitterName: 'Submitter Two',
      strategicThemeId: 'theme-1', evaluationAggregateScore: null, updatedAt: '2026-01-02',
    };
    const promise = service.getCommitteePendingQueue();
    const req = httpMock.expectOne('/api/supervisor/committee-pending-queue');
    expect(req.request.method).toBe('GET');
    req.flush([item]);

    expect(await promise).toEqual([item]);
  });
});

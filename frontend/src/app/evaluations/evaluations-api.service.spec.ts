import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { EvaluationsApiService } from './evaluations-api.service';
import { EvaluationInput } from './evaluation.model';

describe('EvaluationsApiService', () => {
  let service: EvaluationsApiService;
  let httpMock: HttpTestingController;

  const sampleInput: EvaluationInput = {
    innovation: 7, impact: 7, execution: 7, scalability: 7, presentation: 7, comments: 'Good idea.',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(EvaluationsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('submit() posts to /api/ideas/{id}/evaluations and returns the response', async () => {
    const promise = service.submit('idea-1', sampleInput);
    const req = httpMock.expectOne('/api/ideas/idea-1/evaluations');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(sampleInput);
    req.flush({ id: 'eval-1', totalScore: 7, recommendation: 'pass', ideaStatus: 'pass_awaiting_attachments' });

    expect(await promise).toEqual({ id: 'eval-1', totalScore: 7, recommendation: 'pass', ideaStatus: 'pass_awaiting_attachments' });
  });

  it('getQueue() gets /api/evaluations/queue', async () => {
    const promise = service.getQueue();
    const req = httpMock.expectOne('/api/evaluations/queue');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'T', submitterName: 'Submitter One', strategicThemeId: 'theme-1', updatedAt: '2026-01-01' }]);

    expect(await promise).toEqual([{ id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'T', submitterName: 'Submitter One', strategicThemeId: 'theme-1', updatedAt: '2026-01-01' }]);
  });

  it('getMine() gets /api/evaluations/mine', async () => {
    const promise = service.getMine();
    const req = httpMock.expectOne('/api/evaluations/mine');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'eval-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'T', totalScore: 7, recommendation: 'pass', submittedAt: '2026-01-01', ideaEnteredEvaluationAt: null }]);

    expect(await promise).toEqual([{ id: 'eval-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'T', totalScore: 7, recommendation: 'pass', submittedAt: '2026-01-01', ideaEnteredEvaluationAt: null }]);
  });
});

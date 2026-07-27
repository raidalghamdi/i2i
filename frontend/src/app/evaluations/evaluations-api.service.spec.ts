import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { EvaluationsApiService } from './evaluations-api.service';
import { EvaluationCriterion, EvaluationInput } from './evaluation.model'; // Change 20260726

describe('EvaluationsApiService', () => {
  let service: EvaluationsApiService;
  let httpMock: HttpTestingController;

  // Change 20260726
  const sampleInput: EvaluationInput = {
    criteriaScores: { innovation: 7, impact: 8 },
    comments: 'Good idea.',
    action: 'submit',
    conflictOfInterest: false,
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

  // Change 20260726
  it('submit() sends action "draft" and the criteria-score dictionary unchanged', async () => {
    const promise = service.submit('idea-1', { ...sampleInput, action: 'draft' });
    const req = httpMock.expectOne('/api/ideas/idea-1/evaluations');
    expect(req.request.body.action).toBe('draft');
    expect(req.request.body.criteriaScores).toEqual({ innovation: 7, impact: 8 });
    req.flush({ id: 'eval-1', totalScore: 0, recommendation: 'pending', ideaStatus: 'under_evaluation', submittedAt: null });

    expect((await promise).submittedAt).toBeNull();
  });

  // Change 20260726
  it('getCriteria() gets /api/evaluation-criteria', async () => {
    const criteria: EvaluationCriterion[] = [
      { code: 'innovation', nameAr: 'الابتكار', nameEn: 'Innovation', descriptionAr: null, descriptionEn: null, weight: 0.5, sortOrder: 1 },
    ];
    const promise = service.getCriteria();
    const req = httpMock.expectOne('/api/evaluation-criteria');
    expect(req.request.method).toBe('GET');
    req.flush(criteria);

    expect(await promise).toEqual(criteria);
  });

  it('getQueue() gets /api/evaluations/queue', async () => {
    const promise = service.getQueue();
    const req = httpMock.expectOne('/api/evaluations/queue');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'T', strategicThemeId: 'theme-1', updatedAt: '2026-01-01' }]);

    expect(await promise).toEqual([{ id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'T', strategicThemeId: 'theme-1', updatedAt: '2026-01-01' }]);
  });

  it('getMine() gets /api/evaluations/mine', async () => {
    const promise = service.getMine();
    const req = httpMock.expectOne('/api/evaluations/mine');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'eval-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'T', totalScore: 7, recommendation: 'pass', submittedAt: '2026-01-01', ideaEnteredEvaluationAt: null }]);

    expect(await promise).toEqual([{ id: 'eval-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'T', totalScore: 7, recommendation: 'pass', submittedAt: '2026-01-01', ideaEnteredEvaluationAt: null }]);
  });
});

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { CommitteeApiService } from './committee-api.service';
import { CommitteeDecisionInput } from './committee.model';

describe('CommitteeApiService', () => {
  let service: CommitteeApiService;
  let httpMock: HttpTestingController;

  const sampleInput: CommitteeDecisionInput = {
    decisionTypeCode: 'approved',
    criteriaScores: { originality: 8, feasibility: 8, impact: 8, alignment: 8 },
    comments: 'Good.',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CommitteeApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('submitToCommittee() posts to /api/ideas/{id}/submit-to-committee', async () => {
    const promise = service.submitToCommittee('idea-1');
    const req = httpMock.expectOne('/api/ideas/idea-1/submit-to-committee');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'idea-1', status: 'committee' });

    expect(await promise).toEqual({ id: 'idea-1', status: 'committee' });
  });

  it('getCriteria() gets /api/committee-criteria', async () => {
    const promise = service.getCriteria();
    const req = httpMock.expectOne('/api/committee-criteria');
    expect(req.request.method).toBe('GET');
    req.flush([{ code: 'originality', nameAr: 'أ', nameEn: 'Originality', weight: 0.3 }]);

    expect(await promise).toEqual([{ code: 'originality', nameAr: 'أ', nameEn: 'Originality', weight: 0.3 }]);
  });

  it('submitDecision() posts to /api/ideas/{id}/committee-decisions', async () => {
    const promise = service.submitDecision('idea-1', sampleInput);
    const req = httpMock.expectOne('/api/ideas/idea-1/committee-decisions');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(sampleInput);
    req.flush({ id: 'decision-1', totalScore: 8, ideaStatus: 'committee' });

    expect(await promise).toEqual({ id: 'decision-1', totalScore: 8, ideaStatus: 'committee' });
  });

  it('getQueue() gets /api/committee/queue', async () => {
    const promise = service.getQueue();
    const req = httpMock.expectOne('/api/committee/queue');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'T', submitterName: 'Submitter One', hasDecided: false, decidedCount: 0, totalJudges: 2, updatedAt: '2026-01-01' }]);

    expect(await promise).toEqual([{ id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'T', submitterName: 'Submitter One', hasDecided: false, decidedCount: 0, totalJudges: 2, updatedAt: '2026-01-01' }]);
  });

  it('getMine() gets /api/committee/mine', async () => {
    const promise = service.getMine();
    const req = httpMock.expectOne('/api/committee/mine');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'decision-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'T', totalScore: 8, decidedAt: '2026-01-01' }]);

    expect(await promise).toEqual([{ id: 'decision-1', ideaId: 'idea-1', ideaCode: 'IDEA-0001', ideaTitleEn: 'T', totalScore: 8, decidedAt: '2026-01-01' }]);
  });
});

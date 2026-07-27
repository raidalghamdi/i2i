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

  it('submitToCommittee() posts judgeIds to /api/ideas/{id}/submit-to-committee', async () => {
    const promise = service.submitToCommittee('idea-1', ['judge-1', 'judge-2']);
    const req = httpMock.expectOne('/api/ideas/idea-1/submit-to-committee');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ judgeIds: ['judge-1', 'judge-2'] });
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

  // Change 20260726
  it('submitDecision() posts multipart form-data to /api/ideas/{id}/committee-decisions', async () => {
    const promise = service.submitDecision('idea-1', sampleInput);
    const req = httpMock.expectOne('/api/ideas/idea-1/committee-decisions');
    expect(req.request.method).toBe('POST');
    const body = req.request.body as FormData;
    expect(body instanceof FormData).toBeTrue();
    expect(body.get('decisionType')).toBe('approved');
    expect(JSON.parse(body.get('criteriaScores') as string)).toEqual(sampleInput.criteriaScores);
    expect(body.get('comments')).toBe('Good.');
    expect(body.getAll('attachments').length).toBe(0);
    req.flush({ id: 'decision-1', totalScore: 8, ideaStatus: 'committee' });

    expect(await promise).toEqual({ id: 'decision-1', totalScore: 8, ideaStatus: 'committee' });
  });

  // Change 20260726
  it('submitDecision() appends each selected file under the "attachments" field', async () => {
    const files = [
      new File(['a'], 'minutes.pdf', { type: 'application/pdf' }),
      new File(['b'], 'chart.png', { type: 'image/png' }),
    ];
    const promise = service.submitDecision('idea-1', sampleInput, files);
    const req = httpMock.expectOne('/api/ideas/idea-1/committee-decisions');
    const body = req.request.body as FormData;
    const attached = body.getAll('attachments') as File[];
    expect(attached.length).toBe(2);
    expect(attached.map((f) => f.name)).toEqual(['minutes.pdf', 'chart.png']);
    req.flush({ id: 'decision-1', totalScore: 8, ideaStatus: 'committee', attachments: [] });

    expect((await promise).id).toBe('decision-1');
  });

  // Change 20260726
  it('submitDecision() omits comments when they are empty', async () => {
    const promise = service.submitDecision('idea-1', { ...sampleInput, comments: null });
    const req = httpMock.expectOne('/api/ideas/idea-1/committee-decisions');
    expect((req.request.body as FormData).has('comments')).toBeFalse();
    req.flush({ id: 'decision-1', totalScore: 8, ideaStatus: 'committee' });
    await promise;
  });

  // Change 20260726
  it('getAttachment() gets the attachment as a blob', async () => {
    const promise = service.getAttachment('decision-1', 'attachment-1');
    const req = httpMock.expectOne('/api/committee-decisions/decision-1/attachments/attachment-1');
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    const blob = new Blob(['content'], { type: 'application/pdf' });
    req.flush(blob);

    expect(await promise).toBe(blob);
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

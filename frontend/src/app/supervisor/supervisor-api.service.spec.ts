import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { SupervisorApiService } from './supervisor-api.service';

describe('SupervisorApiService', () => {
  let service: SupervisorApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SupervisorApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getScreeningQueue() gets /api/screening/queue', async () => {
    const promise = service.getScreeningQueue();
    const req = httpMock.expectOne('/api/screening/queue');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'T', submitterName: 'Submitter One', strategicThemeId: 'theme-1', updatedAt: '2026-01-01' }]);

    expect(await promise).toEqual([{ id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'T', submitterName: 'Submitter One', strategicThemeId: 'theme-1', updatedAt: '2026-01-01' }]);
  });

  it('submitScreeningDecision() posts to /api/ideas/{id}/screening-decision', async () => {
    const promise = service.submitScreeningDecision('idea-1', { decisionCode: 'approve', reason: null });
    const req = httpMock.expectOne('/api/ideas/idea-1/screening-decision');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ decisionCode: 'approve', reason: null });
    req.flush({ id: 'idea-1', status: 'evaluation' });

    expect(await promise).toEqual({ id: 'idea-1', status: 'evaluation' });
  });

  it('getTrackAssignments() gets /api/track-assignments', async () => {
    const promise = service.getTrackAssignments();
    const req = httpMock.expectOne('/api/track-assignments');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'assign-1', evaluatorId: 'eval-1', evaluatorName: 'Evaluator One', trackId: 'theme-1', trackNameEn: 'Track One' }]);

    expect(await promise).toEqual([{ id: 'assign-1', evaluatorId: 'eval-1', evaluatorName: 'Evaluator One', trackId: 'theme-1', trackNameEn: 'Track One' }]);
  });

  it('createTrackAssignment() posts to /api/track-assignments', async () => {
    const promise = service.createTrackAssignment({ evaluatorId: 'eval-1', trackId: 'theme-1' });
    const req = httpMock.expectOne('/api/track-assignments');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ evaluatorId: 'eval-1', trackId: 'theme-1' });
    req.flush({ id: 'assign-1' });

    expect(await promise).toEqual({ id: 'assign-1' });
  });

  it('removeTrackAssignment() deletes /api/track-assignments/{id}', async () => {
    const promise = service.removeTrackAssignment('assign-1');
    const req = httpMock.expectOne('/api/track-assignments/assign-1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    await promise;
  });

  it('getUsersByRole() gets /api/users?role={role}', async () => {
    const promise = service.getUsersByRole('evaluator');
    const req = httpMock.expectOne('/api/users?role=evaluator');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'eval-1', fullNameAr: 'أ', fullNameEn: 'Evaluator One' }]);

    expect(await promise).toEqual([{ id: 'eval-1', fullNameAr: 'أ', fullNameEn: 'Evaluator One' }]);
  });

  it('previewFinalRanking() gets /api/final-ranking/preview', async () => {
    const promise = service.previewFinalRanking();
    const req = httpMock.expectOne('/api/final-ranking/preview');
    expect(req.request.method).toBe('GET');
    req.flush({ approvedCount: 1, notSelectedCount: 0, topN: 5, entries: [] });

    expect(await promise).toEqual({ approvedCount: 1, notSelectedCount: 0, topN: 5, entries: [] });
  });

  it('runFinalRanking() posts to /api/final-ranking/run', async () => {
    const promise = service.runFinalRanking();
    const req = httpMock.expectOne('/api/final-ranking/run');
    expect(req.request.method).toBe('POST');
    req.flush({ approvedCount: 1, notSelectedCount: 0, topN: 5, entries: [] });

    expect(await promise).toEqual({ approvedCount: 1, notSelectedCount: 0, topN: 5, entries: [] });
  });
});

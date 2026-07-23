import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { IdeasApiService } from './ideas-api.service';
import { IdeaInput, IdeaListPage, MyIdeaItem } from './idea.model';

describe('IdeasApiService', () => {
  let service: IdeasApiService;
  let httpMock: HttpTestingController;

  const sampleInput: IdeaInput = {
    titleAr: 'ا', titleEn: 'Title', problemStatementAr: 'م', problemStatementEn: 'Problem',
    proposedSolutionAr: 'ح', proposedSolutionEn: 'Solution', expectedBenefitsAr: 'ف', expectedBenefitsEn: 'Benefits',
    strategicThemeId: 'theme-1', activityId: 'activity-1', challengeId: null,
    participationType: 'individual', teamName: null, teamMembers: [],
    ipAcknowledged: true, termsAgreed: true,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(IdeasApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('create() posts to /api/ideas and returns the response', async () => {
    const promise = service.create(sampleInput);
    const req = httpMock.expectOne('/api/ideas');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(sampleInput);
    req.flush({ id: 'idea-1', code: 'IDEA-0001', status: 'draft' });

    expect(await promise).toEqual({ id: 'idea-1', code: 'IDEA-0001', status: 'draft' });
  });

  it('update() puts to /api/ideas/{id}', async () => {
    const promise = service.update('idea-1', sampleInput);
    const req = httpMock.expectOne('/api/ideas/idea-1');
    expect(req.request.method).toBe('PUT');
    req.flush({ id: 'idea-1', code: 'IDEA-0001' });

    expect(await promise).toEqual({ id: 'idea-1', code: 'IDEA-0001' });
  });

  it('submit() posts to /api/ideas/{id}/submit', async () => {
    const promise = service.submit('idea-1');
    const req = httpMock.expectOne('/api/ideas/idea-1/submit');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'idea-1', status: 'submitted' });

    expect(await promise).toEqual({ id: 'idea-1', status: 'submitted' });
  });

  it('getMine() gets /api/ideas/mine', async () => {
    const promise = service.getMine();
    const req = httpMock.expectOne('/api/ideas/mine');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'T', status: 'draft', updatedAt: '2026-01-01' }]);

    expect(await promise).toEqual([{ id: 'idea-1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'T', status: 'draft', updatedAt: '2026-01-01' }]);
  });

  it('getById() gets /api/ideas/{id}', async () => {
    const promise = service.getById('idea-1');
    const req = httpMock.expectOne('/api/ideas/idea-1');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'idea-1', attachments: [] });

    expect(await promise).toEqual(jasmine.objectContaining({ id: 'idea-1' }));
  });

  it('uploadAttachment() posts multipart form data to /api/ideas/{id}/attachments', async () => {
    const file = new File(['content'], 'a.pdf', { type: 'application/pdf' });
    const promise = service.uploadAttachment('idea-1', file);
    const req = httpMock.expectOne('/api/ideas/idea-1/attachments');
    expect(req.request.method).toBe('POST');
    expect(req.request.body instanceof FormData).toBe(true);
    const formData = req.request.body as FormData;
    expect(formData.get('file')).toBe(file);
    req.flush({ id: 'att-1', fileName: 'a.pdf', contentType: 'application/pdf', fileSizeBytes: 7, uploadedAt: '2026-01-01' });

    expect(await promise).toEqual({ id: 'att-1', fileName: 'a.pdf', contentType: 'application/pdf', fileSizeBytes: 7, uploadedAt: '2026-01-01' });
  });

  it('getAttachments() gets /api/ideas/{id}/attachments', async () => {
    const promise = service.getAttachments('idea-1');
    const req = httpMock.expectOne('/api/ideas/idea-1/attachments');
    expect(req.request.method).toBe('GET');
    req.flush([]);

    expect(await promise).toEqual([]);
  });

  it('getJourney() gets /api/ideas/{id}/journey', async () => {
    const promise = service.getJourney('idea-1');
    const req = httpMock.expectOne('/api/ideas/idea-1/journey');
    expect(req.request.method).toBe('GET');
    req.flush({ currentStage: 3, stopped: false, evaluationScore: 7, stages: [] });

    expect(await promise).toEqual(jasmine.objectContaining({ currentStage: 3 }));
  });

  it('list() gets /api/ideas with only the provided filters as query params', async () => {
    const promise = service.list({ q: 'solar', status: 'approved', page: 2 });
    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/ideas' &&
        r.params.get('q') === 'solar' &&
        r.params.get('status') === 'approved' &&
        r.params.get('page') === '2' &&
        !r.params.has('strategicThemeId') &&
        !r.params.has('activityId') &&
        !r.params.has('stage'),
    );
    expect(req.request.method).toBe('GET');
    const page: IdeaListPage = {
      items: [
        {
          id: 'idea-1',
          code: 'IDEA-0001',
          titleAr: 'ا',
          titleEn: 'Solar Panel',
          problemStatementAr: 'م',
          problemStatementEn: 'Problem',
          currentStage: 2,
          status: 'approved',
          strategicThemeId: 'theme-1',
          activityId: 'activity-1',
        },
      ],
      total: 1,
      page: 2,
      pageSize: 20,
    };
    req.flush(page);

    const result = await promise;
    expect(result.items).toEqual(page.items);
    expect(result.total).toBe(1);
  });

  it('getMineDetailed() gets /api/ideas/mine with a statusGroup param when provided', async () => {
    const promise = service.getMineDetailed('in_review');
    const req = httpMock.expectOne(
      (r) => r.url === '/api/ideas/mine' && r.params.get('statusGroup') === 'in_review',
    );
    expect(req.request.method).toBe('GET');
    const items: MyIdeaItem[] = [
      {
        id: 'idea-1',
        code: 'IDEA-0001',
        titleAr: 'ا',
        titleEn: 'T',
        status: 'in_review',
        currentStage: 2,
        createdAt: '2026-01-01',
        updatedAt: '2026-01-02',
        feedbackCount: 3,
      },
    ];
    req.flush(items);

    expect(await promise).toEqual(items);
  });

  it('getMineDetailed() gets /api/ideas/mine with no statusGroup param when omitted', async () => {
    const promise = service.getMineDetailed();
    const req = httpMock.expectOne((r) => r.url === '/api/ideas/mine' && !r.params.has('statusGroup'));
    expect(req.request.method).toBe('GET');
    req.flush([]);

    expect(await promise).toEqual([]);
  });

  it('withdraw() posts to /api/ideas/{id}/withdraw', async () => {
    const promise = service.withdraw('idea-1');
    const req = httpMock.expectOne('/api/ideas/idea-1/withdraw');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush(null);

    await expectAsync(promise).toBeResolved();
  });
});

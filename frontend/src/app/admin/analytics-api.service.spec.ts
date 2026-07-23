import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AnalyticsApiService } from './analytics-api.service';
import { AnalyticsDashboard, ExecutiveAnalytics, PillarDetail } from './analytics.model';

describe('AnalyticsApiService', () => {
  let service: AnalyticsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(AnalyticsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('fetches the dashboard via GET /api/admin/analytics', async () => {
    const sample: AnalyticsDashboard = {
      platformKpis: { totalIdeas: 1, totalApproved: 0, totalSubmitters: 1, totalEvaluations: 0, totalEvaluators: 0 },
      ideasByStatus: [],
      submissionsOverTime: [],
      themeActivity: [],
      topEvaluators: [],
      slaCompliance: { compliancePct: null, totalTracked: 0 },
    };

    const promise = service.getDashboard();
    const req = httpMock.expectOne('/api/admin/analytics');
    expect(req.request.method).toBe('GET');
    req.flush(sample);

    await expectAsync(promise).toBeResolvedTo(sample);
  });

  it('fetches executive analytics via GET /api/analytics/executive', async () => {
    const sample: ExecutiveAnalytics = {
      kpis: {
        totalSubmissions: 10,
        totalApproved: 5,
        totalImplemented: 2,
        activeSubmitters: 8,
        totalEvaluations: 20,
        totalUsers: 30,
        totalEvaluators: 4,
        realizedFinancialImpact: 15000,
      },
      funnel: [{ stageKey: 'submitted', count: 10 }],
      cohort: [{ month: '2026-01', submitted: 10, approved: 5, rejected: 2, implemented: 1 }],
      ideasByStage: [{ stage: 2, count: 3 }],
      submissions: [{ date: '2026-01-01', count: 4 }],
      topObjectives: [{ themeId: 't1', nameAr: 'موضوع', nameEn: 'Theme', count: 6 }],
      avgTimePerStage: [{ stage: 2, avgDays: 3.5 }],
      conversion: { submitted: 10, pilot: 3, rate: 0.3 },
    };

    const promise = service.getExecutive();
    const req = httpMock.expectOne('/api/analytics/executive');
    expect(req.request.method).toBe('GET');
    req.flush(sample);

    await expectAsync(promise).toBeResolvedTo(sample);
  });

  it('fetches pillar detail via GET /api/analytics/pillars/:themeId', async () => {
    const sample: PillarDetail = {
      themeId: 't1',
      nameAr: 'موضوع',
      nameEn: 'Theme',
      descriptionAr: 'وصف',
      descriptionEn: 'Description',
      ownerName: 'Jane Doe',
      kpis: { ideas: 12, budgetSpent: 1000, budgetAllocated: 5000, pilotsActive: 2, implementationsDone: 1 },
      timeline: [{ month: '2026-01', count: 4 }],
      ideas: [{ id: 'i1', code: 'IDEA-1', titleAr: 'فكرة', titleEn: 'Idea', status: 'submitted', currentStage: 'review' }],
    };

    const promise = service.getPillar('t1');
    const req = httpMock.expectOne('/api/analytics/pillars/t1');
    expect(req.request.method).toBe('GET');
    req.flush(sample);

    await expectAsync(promise).toBeResolvedTo(sample);
  });

  it('returns null when the pillar is not found (404)', async () => {
    const promise = service.getPillar('missing');
    httpMock.expectOne('/api/analytics/pillars/missing').flush('', { status: 404, statusText: 'Not Found' });
    expect(await promise).toBeNull();
  });

  it('exports analytics via POST /api/admin/analytics/export?format=xlsx', async () => {
    const promise = service.exportAnalytics('xlsx');
    const req = httpMock.expectOne((r) => r.url === '/api/admin/analytics/export' && r.params.get('format') === 'xlsx');
    expect(req.request.method).toBe('POST');
    req.flush({ reportGenerationId: 'r1', status: 'pending' });

    await expectAsync(promise).toBeResolvedTo({ reportGenerationId: 'r1', status: 'pending' });
  });

  it('downloads a report as a blob via GET /api/admin/reports/:id/download', async () => {
    const promise = service.downloadReport('r1');
    const req = httpMock.expectOne('/api/admin/reports/r1/download');
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    const blob = new Blob(['fake xlsx content']);
    req.flush(blob);
    const result = await promise;
    expect(result instanceof Blob).toBe(true);
  });
});

import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ReportsApiService } from './reports-api.service';

describe('ReportsApiService', () => {
  let service: ReportsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(ReportsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('generates the audit log report via POST /api/admin/reports/audit-log/generate', async () => {
    const promise = service.generateAuditLogReport();
    const req = httpMock.expectOne('/api/admin/reports/audit-log/generate');
    expect(req.request.method).toBe('POST');
    req.flush({ reportGenerationId: 'r1', status: 'completed', fileUrl: '/tmp/a.xlsx' });
    await expectAsync(promise).toBeResolvedTo({ reportGenerationId: 'r1', status: 'completed', fileUrl: '/tmp/a.xlsx' });
  });

  it('generates the ideas report via POST /api/admin/reports/ideas/generate', async () => {
    const promise = service.generateIdeasReport();
    const req = httpMock.expectOne('/api/admin/reports/ideas/generate');
    expect(req.request.method).toBe('POST');
    req.flush({ reportGenerationId: 'r2', status: 'completed', fileUrl: '/tmp/b.xlsx' });
    await promise;
  });

  it('generates the evaluations report via POST /api/admin/reports/evaluations/generate', async () => {
    const promise = service.generateEvaluationsReport();
    const req = httpMock.expectOne('/api/admin/reports/evaluations/generate');
    expect(req.request.method).toBe('POST');
    req.flush({ reportGenerationId: 'r3', status: 'completed', fileUrl: '/tmp/c.xlsx' });
    await promise;
  });

  it('generates the escalations report via POST /api/admin/reports/escalations/generate', async () => {
    const promise = service.generateEscalationsReport();
    const req = httpMock.expectOne('/api/admin/reports/escalations/generate');
    expect(req.request.method).toBe('POST');
    req.flush({ reportGenerationId: 'r4', status: 'completed', fileUrl: '/tmp/d.xlsx' });
    await promise;
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

  it('generates a generic report via POST /api/admin/reports/generate with type + opts + default format', async () => {
    const promise = service.generateReport('executive', { from: '2026-01-01' });
    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/admin/reports/generate' &&
        r.params.get('type') === 'executive' &&
        r.params.get('from') === '2026-01-01' &&
        r.params.get('format') === 'xlsx' &&
        !r.params.has('to') &&
        !r.params.has('themeId'),
    );
    expect(req.request.method).toBe('POST');
    req.flush({ reportGenerationId: 'r5', status: 'completed', fileUrl: '/tmp/e.xlsx' });
    await expectAsync(promise).toBeResolvedTo({ reportGenerationId: 'r5', status: 'completed', fileUrl: '/tmp/e.xlsx' });
  });

  it('generates a generic report with no opts, sending only type + default format', async () => {
    const promise = service.generateReport('audit');
    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/admin/reports/generate' &&
        r.params.get('type') === 'audit' &&
        r.params.get('format') === 'xlsx' &&
        !r.params.has('from') &&
        !r.params.has('to') &&
        !r.params.has('themeId'),
    );
    expect(req.request.method).toBe('POST');
    req.flush({ reportGenerationId: 'r6', status: 'completed', fileUrl: null });
    await promise;
  });

  it('generates a generic report forwarding an explicit format instead of the xlsx default', async () => {
    const promise = service.generateReport('executive', { format: 'pdf' });
    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/admin/reports/generate' &&
        r.params.get('type') === 'executive' &&
        r.params.get('format') === 'pdf',
    );
    expect(req.request.method).toBe('POST');
    req.flush({ reportGenerationId: 'r8', status: 'completed', fileUrl: '/tmp/g.pdf' });
    await expectAsync(promise).toBeResolvedTo({ reportGenerationId: 'r8', status: 'completed', fileUrl: '/tmp/g.pdf' });
  });

  it('exports analytics via POST /api/admin/analytics/export with format param', async () => {
    const promise = service.exportAnalytics('pdf');
    const req = httpMock.expectOne(
      (r) => r.url === '/api/admin/analytics/export' && r.params.get('format') === 'pdf',
    );
    expect(req.request.method).toBe('POST');
    req.flush({ reportGenerationId: 'r7', status: 'completed', fileUrl: '/tmp/f.pdf' });
    await expectAsync(promise).toBeResolvedTo({ reportGenerationId: 'r7', status: 'completed', fileUrl: '/tmp/f.pdf' });
  });
});

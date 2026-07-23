import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuditApiService } from './audit-api.service';
import { AuditBrowseResult } from './audit.model';

describe('AuditApiService', () => {
  let service: AuditApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(AuditApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('browses via GET /api/admin/audit with no params when filter is empty', async () => {
    const promise = service.browse({});
    const req = httpMock.expectOne('/api/admin/audit');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    const result: AuditBrowseResult = { items: [], total: 0, page: 1, pageSize: 20 };
    req.flush(result);
    await expectAsync(promise).toBeResolvedTo(result);
  });

  it('browses via GET /api/admin/audit with all filter params set', async () => {
    const promise = service.browse({
      entityType: 'idea',
      action: 'update',
      actorId: 'u1',
      from: '2026-01-01',
      to: '2026-01-31',
      page: 2,
      pageSize: 50,
    });
    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/admin/audit' &&
        r.params.get('entityType') === 'idea' &&
        r.params.get('action') === 'update' &&
        r.params.get('actorId') === 'u1' &&
        r.params.get('from') === '2026-01-01' &&
        r.params.get('to') === '2026-01-31' &&
        r.params.get('page') === '2' &&
        r.params.get('pageSize') === '50',
    );
    expect(req.request.method).toBe('GET');
    const result: AuditBrowseResult = {
      items: [
        {
          id: 'a1',
          chainSeq: 1,
          occurredAt: '2026-01-05T00:00:00Z',
          actorName: 'Jane Doe',
          entityType: 'idea',
          entityId: 'i1',
          action: 'update',
          verified: true,
        },
      ],
      total: 1,
      page: 2,
      pageSize: 50,
    };
    req.flush(result);
    await expectAsync(promise).toBeResolvedTo(result);
  });

  it('omits page/pageSize params when not provided', async () => {
    const promise = service.browse({ entityType: 'idea' });
    const req = httpMock.expectOne(
      (r) => r.url === '/api/admin/audit' && r.params.get('entityType') === 'idea' && !r.params.has('page') && !r.params.has('pageSize'),
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 20 });
    await promise;
  });

  it('treats page 0 as a valid provided value', async () => {
    const promise = service.browse({ page: 0 });
    const req = httpMock.expectOne((r) => r.url === '/api/admin/audit' && r.params.get('page') === '0');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 0, pageSize: 20 });
    await promise;
  });
});

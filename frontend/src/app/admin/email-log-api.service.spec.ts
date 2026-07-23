import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { EmailLogApiService } from './email-log-api.service';
import { EmailLogListResult } from './email-log.model';

describe('EmailLogApiService', () => {
  let service: EmailLogApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(EmailLogApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists via GET /api/admin/email-log with no params when filter is empty', async () => {
    const promise = service.list({});
    const req = httpMock.expectOne('/api/admin/email-log');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    const result: EmailLogListResult = { items: [], total: 0, page: 1, pageSize: 20 };
    req.flush(result);
    await expectAsync(promise).toBeResolvedTo(result);
  });

  it('lists via GET /api/admin/email-log with page, pageSize, status set', async () => {
    const promise = service.list({ page: 3, pageSize: 25, status: 'failed' });
    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/admin/email-log' &&
        r.params.get('page') === '3' &&
        r.params.get('pageSize') === '25' &&
        r.params.get('status') === 'failed',
    );
    expect(req.request.method).toBe('GET');
    const result: EmailLogListResult = {
      items: [
        {
          id: 'e1',
          provider: 'smtp',
          statusCode: 'failed',
          statusNameAr: 'فشل',
          statusNameEn: 'Failed',
          providerMessageId: null,
          redirectApplied: false,
          toEmail: 'a@example.com',
          sentAt: '2026-01-05T00:00:00Z',
        },
      ],
      total: 1,
      page: 3,
      pageSize: 25,
    };
    req.flush(result);
    await expectAsync(promise).toBeResolvedTo(result);
  });

  it('omits status param when not provided', async () => {
    const promise = service.list({ page: 1, pageSize: 10 });
    const req = httpMock.expectOne(
      (r) => r.url === '/api/admin/email-log' && r.params.get('page') === '1' && !r.params.has('status'),
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 10 });
    await promise;
  });
});

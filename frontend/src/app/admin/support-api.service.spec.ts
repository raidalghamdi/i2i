import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SupportApiService } from './support-api.service';
import { SupportListResult } from './support.model';

describe('SupportApiService', () => {
  let service: SupportApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(SupportApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists via GET /api/admin/support with no params when filter is empty', async () => {
    const promise = service.list({});
    const req = httpMock.expectOne('/api/admin/support');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    const result: SupportListResult = { items: [], total: 0, page: 1, pageSize: 20 };
    req.flush(result);
    await expectAsync(promise).toBeResolvedTo(result);
  });

  it('lists via GET /api/admin/support with page, pageSize, handled set', async () => {
    const promise = service.list({ page: 2, pageSize: 15, handled: false });
    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/admin/support' &&
        r.params.get('page') === '2' &&
        r.params.get('pageSize') === '15' &&
        r.params.get('handled') === 'false',
    );
    expect(req.request.method).toBe('GET');
    const result: SupportListResult = {
      items: [
        {
          id: 's1',
          name: 'Jane Doe',
          email: 'jane@example.com',
          subject: 'Help',
          body: 'I need help',
          handled: false,
          createdAt: '2026-01-05T00:00:00Z',
        },
      ],
      total: 1,
      page: 2,
      pageSize: 15,
    };
    req.flush(result);
    await expectAsync(promise).toBeResolvedTo(result);
  });

  it('omits handled param when not provided', async () => {
    const promise = service.list({ page: 1, pageSize: 10 });
    const req = httpMock.expectOne(
      (r) => r.url === '/api/admin/support' && r.params.get('page') === '1' && !r.params.has('handled'),
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 10 });
    await promise;
  });

  it('marks handled via POST /api/admin/support/:id/handled', async () => {
    const promise = service.markHandled('s1');
    const req = httpMock.expectOne('/api/admin/support/s1/handled');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush(null);
    await promise;
  });
});

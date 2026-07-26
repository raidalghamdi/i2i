import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { EscalationsApiService } from './escalations-api.service';

describe('EscalationsApiService', () => {
  let service: EscalationsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(EscalationsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists escalations via GET /api/admin/escalations with query params', async () => {
    const promise = service.list({ status: 'open', tier: undefined, entityType: undefined });
    const req = httpMock.expectOne((r) => r.url === '/api/admin/escalations' && r.params.get('status') === 'open');
    req.flush([]);
    await expectAsync(promise).toBeResolvedTo([]);
  });

  it('acknowledges via POST /api/admin/escalations/:id/acknowledge', async () => {
    const promise = service.acknowledge('e1', { notes: 'checking' });
    const req = httpMock.expectOne('/api/admin/escalations/e1/acknowledge');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ notes: 'checking' });
    req.flush({ id: 'e1', statusCode: 'acknowledged' });
    await promise;
  });

  it('bumps via POST /api/admin/escalations/:id/bump', async () => {
    const promise = service.bump('e1', { notes: null });
    const req = httpMock.expectOne('/api/admin/escalations/e1/bump');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'e1', tierCode: 'director', statusCode: 'open' });
    await promise;
  });

  it('resolves via POST /api/admin/escalations/:id/resolve', async () => {
    const promise = service.resolve('e1', { resolutionAr: 'تم', resolutionEn: 'fixed' });
    const req = httpMock.expectOne('/api/admin/escalations/e1/resolve');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'e1', statusCode: 'resolved' });
    await promise;
  });
});

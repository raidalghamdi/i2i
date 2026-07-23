import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ReportTitlesApiService } from './report-titles-api.service';

describe('ReportTitlesApiService', () => {
  let service: ReportTitlesApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(ReportTitlesApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists report titles via GET /api/admin/report-titles', async () => {
    const promise = service.list();
    const req = httpMock.expectOne('/api/admin/report-titles');
    expect(req.request.method).toBe('GET');
    const titles = [
      { id: 't1', key: 'executive', titleAr: 'تنفيذي', titleEn: 'Executive', sortOrder: 1 },
    ];
    req.flush(titles);
    await expectAsync(promise).toBeResolvedTo(titles);
  });

  it('creates a report title via POST /api/admin/report-titles', async () => {
    const input = { key: 'detailed', titleAr: 'مفصل', titleEn: 'Detailed', sortOrder: 2 };
    const promise = service.create(input);
    const req = httpMock.expectOne('/api/admin/report-titles');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(input);
    req.flush({ id: 't2' });
    await expectAsync(promise).toBeResolvedTo({ id: 't2' });
  });

  it('updates a report title via PUT /api/admin/report-titles/:id', async () => {
    const patch = { titleAr: 'محدث', titleEn: 'Updated', sortOrder: 3 };
    const promise = service.update('t2', patch);
    const req = httpMock.expectOne('/api/admin/report-titles/t2');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(patch);
    req.flush(null);
    await promise;
  });

  it('removes a report title via DELETE /api/admin/report-titles/:id', async () => {
    const promise = service.remove('t2');
    const req = httpMock.expectOne('/api/admin/report-titles/t2');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
    await promise;
  });
});

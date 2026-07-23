import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CommitteeCriteriaApiService } from './committee-criteria-api.service';
import { CommitteeCriterionInput } from './committee-criteria.model';

describe('CommitteeCriteriaApiService', () => {
  let service: CommitteeCriteriaApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(CommitteeCriteriaApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists criteria via GET /api/admin/committee-criteria', async () => {
    const promise = service.list();
    const req = httpMock.expectOne('/api/admin/committee-criteria');
    expect(req.request.method).toBe('GET');
    req.flush([]);
    await expectAsync(promise).toBeResolvedTo([]);
  });

  it('creates a criterion via POST /api/admin/committee-criteria', async () => {
    const input: CommitteeCriterionInput = {
      code: 'innovation',
      nameAr: 'الابتكار',
      nameEn: 'Innovation',
      descriptionAr: null,
      descriptionEn: null,
      weight: 25,
      active: true,
    };
    const promise = service.create(input);
    const req = httpMock.expectOne('/api/admin/committee-criteria');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(input);
    req.flush({ id: 'c1', ...input });
    await promise;
  });

  it('updates a criterion via PUT /api/admin/committee-criteria/:id', async () => {
    const input: CommitteeCriterionInput = {
      code: 'impact',
      nameAr: 'الأثر',
      nameEn: 'Impact',
      descriptionAr: 'وصف',
      descriptionEn: 'desc',
      weight: 40,
      active: false,
    };
    const promise = service.update('c1', input);
    const req = httpMock.expectOne('/api/admin/committee-criteria/c1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(input);
    req.flush({ id: 'c1', ...input });
    await promise;
  });

  it('deletes a criterion via DELETE /api/admin/committee-criteria/:id', async () => {
    const promise = service.remove('c1');
    const req = httpMock.expectOne('/api/admin/committee-criteria/c1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
    await promise;
  });
});

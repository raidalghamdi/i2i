import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { RolesCatalogApiService } from './roles-catalog-api.service';

describe('RolesCatalogApiService', () => {
  let service: RolesCatalogApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(RolesCatalogApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists roles via GET /api/admin/roles', async () => {
    const promise = service.list();
    const req = httpMock.expectOne('/api/admin/roles');
    expect(req.request.method).toBe('GET');
    req.flush([]);
    await expectAsync(promise).toBeResolvedTo([]);
  });

  it('patches only the provided fields via PATCH /api/admin/roles/:id', async () => {
    const promise = service.patch('r1', { nameAr: 'مقيّم' });
    const req = httpMock.expectOne('/api/admin/roles/r1');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ nameAr: 'مقيّم' });
    req.flush({
      id: 'r1',
      code: 'evaluator',
      nameAr: 'مقيّم',
      nameEn: 'Evaluator',
      descriptionAr: null,
      descriptionEn: null,
      isSystem: false,
      isActive: true,
      sortOrder: 1,
    });
    await promise;
  });

  it('patches multiple fields together', async () => {
    const promise = service.patch('r2', { isActive: false, sortOrder: 5 });
    const req = httpMock.expectOne('/api/admin/roles/r2');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ isActive: false, sortOrder: 5 });
    req.flush({
      id: 'r2',
      code: 'reviewer',
      nameAr: 'مراجع',
      nameEn: 'Reviewer',
      descriptionAr: null,
      descriptionEn: null,
      isSystem: true,
      isActive: false,
      sortOrder: 5,
    });
    await promise;
  });
});

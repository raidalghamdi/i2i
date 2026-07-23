import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ComplianceApiService } from './compliance-api.service';
import { ComplianceControlRow } from './compliance.model';

describe('ComplianceApiService', () => {
  let service: ComplianceApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(ComplianceApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists via GET /api/admin/compliance', async () => {
    const promise = service.list();
    const req = httpMock.expectOne('/api/admin/compliance');
    expect(req.request.method).toBe('GET');
    const result: ComplianceControlRow[] = [
      {
        id: 'c1',
        controlCode: 'ISO-1',
        standardBodyCode: 'iso27001',
        standardBodyNameAr: 'آيزو',
        standardBodyNameEn: 'ISO 27001',
        titleAr: 'عنوان',
        titleEn: 'Title',
        descriptionAr: 'وصف',
        descriptionEn: 'Description',
        statusCode: 'compliant',
        statusNameAr: 'ملتزم',
        statusNameEn: 'Compliant',
        mappedFeaturePaths: ['/admin/audit'],
      },
    ];
    req.flush(result);
    await expectAsync(promise).toBeResolvedTo(result);
  });
});

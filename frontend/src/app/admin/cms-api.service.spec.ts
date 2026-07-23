import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CmsApiService } from './cms-api.service';

describe('CmsApiService', () => {
  let service: CmsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(CmsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists blocks via GET /api/admin/cms/blocks', async () => {
    const promise = service.listBlocks();
    httpMock.expectOne('/api/admin/cms/blocks').flush([]);
    await expectAsync(promise).toBeResolvedTo([]);
  });

  it('creates a block via POST /api/admin/cms/blocks', async () => {
    const input = { key: 'k', contentAr: 'أ', contentEn: 'E', isPublished: true };
    const promise = service.createBlock(input);
    const req = httpMock.expectOne('/api/admin/cms/blocks');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(input);
    req.flush({ id: '1', ...input, updatedAt: '2026-01-01' });
    await promise;
  });

  it('deletes a content string via DELETE /api/admin/cms/strings/:id', async () => {
    const promise = service.deleteString('s1');
    const req = httpMock.expectOne('/api/admin/cms/strings/s1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
    await promise;
  });

  it('updates page content via PUT /api/admin/cms/content/:id', async () => {
    const input = { slug: 'faq', titleAr: 'أ', titleEn: 'F', bodyAr: 'ب', bodyEn: 'B', isPublished: true };
    const promise = service.updateContent('c1', input);
    const req = httpMock.expectOne('/api/admin/cms/content/c1');
    expect(req.request.method).toBe('PUT');
    req.flush({ id: 'c1', ...input, publishedAt: null, updatedAt: '2026-01-01' });
    await promise;
  });
});

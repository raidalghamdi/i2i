import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PublicContentApiService } from './public-content-api.service';

describe('PublicContentApiService', () => {
  let service: PublicContentApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(PublicContentApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });
  afterEach(() => httpMock.verify());

  it('gets content by slug', async () => {
    const promise = service.getBySlug('about');
    const req = httpMock.expectOne('/api/public/cms/content/about');
    expect(req.request.method).toBe('GET');
    req.flush({ slug: 'about', titleAr: 'ع', titleEn: 'About', bodyAr: 'ب', bodyEn: 'Body' });
    expect((await promise)?.titleEn).toBe('About');
  });

  it('returns null on 404', async () => {
    const promise = service.getBySlug('missing');
    httpMock.expectOne('/api/public/cms/content/missing').flush('', { status: 404, statusText: 'Not Found' });
    expect(await promise).toBeNull();
  });
});

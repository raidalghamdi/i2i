import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { PublicSearchApiService } from './public-search-api.service';

describe('PublicSearchApiService', () => {
  let service: PublicSearchApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PublicSearchApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PublicSearchApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('requests /api/public/search with the query and returns results', async () => {
    const promise = service.search('solar');
    const req = httpMock.expectOne('/api/public/search?q=solar');
    expect(req.request.method).toBe('GET');
    req.flush({ ideas: [{ id: '1', code: 'I-1', titleAr: 'أ', titleEn: 'Solar', status: 'approved' }], tracks: [] });
    const res = await promise;
    expect(res.ideas.length).toBe(1);
    expect(res.tracks.length).toBe(0);
  });

  it('encodes the query and returns empty groups on error', async () => {
    const promise = service.search('a b');
    const req = httpMock.expectOne('/api/public/search?q=a%20b');
    req.flush('boom', { status: 500, statusText: 'Server Error' });
    const res = await promise;
    expect(res.ideas).toEqual([]);
    expect(res.tracks).toEqual([]);
  });
});

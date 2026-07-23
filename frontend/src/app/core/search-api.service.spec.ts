import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { SearchApiService } from './search-api.service';

describe('SearchApiService', () => {
  let service: SearchApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SearchApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SearchApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('requests /api/search with the query and returns results', async () => {
    const promise = service.search('idea');
    const req = httpMock.expectOne('/api/search?q=idea');
    expect(req.request.method).toBe('GET');
    req.flush({
      ideas: [{ type: 'idea', id: '1', titleEn: 'Idea One', titleAr: 'فكرة', subtitle: 'Submitted', link: '/ideas/1' }],
      challenges: [],
      users: [],
    });
    const res = await promise;
    expect(res.ideas.length).toBe(1);
    expect(res.challenges).toEqual([]);
    expect(res.users).toEqual([]);
  });

  it('returns empty groups without an HTTP call for a blank query', async () => {
    const promise = service.search('   ');
    const res = await promise;
    expect(res).toEqual({ ideas: [], challenges: [], users: [] });
    httpMock.expectNone('/api/search?q=');
    httpMock.expectNone((req) => req.url === '/api/search');
  });
});

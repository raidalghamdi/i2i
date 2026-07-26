import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { DirectoryApiService, DirectoryPerson } from './directory-api.service';

describe('DirectoryApiService', () => {
  let service: DirectoryApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [DirectoryApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(DirectoryApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('requests /api/directory/search with q and a default limit of 10', () => {
    const results: DirectoryPerson[] = [];
    let received: DirectoryPerson[] | undefined;

    service.search('lay').subscribe((res) => (received = res));

    const req = httpMock.expectOne('/api/directory/search?q=lay&limit=10');
    expect(req.request.method).toBe('GET');
    req.flush(results);

    expect(received).toEqual(results);
  });

  it('maps the JSON body to DirectoryPerson[]', () => {
    let received: DirectoryPerson[] | undefined;
    const body = [{ samAccountName: 'jdoe', displayName: 'Jane Doe', email: 'jdoe@example.com' }];

    service.search('doe').subscribe((res) => (received = res));

    httpMock.expectOne('/api/directory/search?q=doe&limit=10').flush(body);

    expect(received).toEqual(body);
  });

  it('honors an explicit limit', () => {
    service.search('doe', 25).subscribe();

    const req = httpMock.expectOne('/api/directory/search?q=doe&limit=25');
    expect(req.request.params.get('limit')).toBe('25');
    req.flush([]);
  });
});

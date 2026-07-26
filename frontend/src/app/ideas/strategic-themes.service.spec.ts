import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { StrategicThemesService } from './strategic-themes.service';

describe('StrategicThemesService', () => {
  let service: StrategicThemesService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(StrategicThemesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list() gets /api/strategic-themes', async () => {
    const promise = service.list();
    const req = httpMock.expectOne('/api/strategic-themes');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'theme-1', nameAr: 'أ', nameEn: 'Theme One' }]);

    expect(await promise).toEqual([{ id: 'theme-1', nameAr: 'أ', nameEn: 'Theme One' }]);
  });
});

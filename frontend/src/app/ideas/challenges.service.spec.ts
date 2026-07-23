import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ChallengesService } from './challenges.service';

describe('ChallengesService', () => {
  let service: ChallengesService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ChallengesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('listByTheme() gets /api/challenges?themeId={id}', async () => {
    const promise = service.listByTheme('theme-1');
    const req = httpMock.expectOne('/api/challenges?themeId=theme-1');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'challenge-1', textAr: 'ت', textEn: 'Challenge One' }]);

    expect(await promise).toEqual([{ id: 'challenge-1', textAr: 'ت', textEn: 'Challenge One' }]);
  });
});

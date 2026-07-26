import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { MeApiService } from './me-api.service';

describe('MeApiService', () => {
  let service: MeApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(MeApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('gets the current profile', async () => {
    const promise = service.get();
    const req = httpMock.expectOne('/api/me');
    expect(req.request.method).toBe('GET');
    req.flush({
      id: 'u1', samAccountName: 's1', email: 's1@x.com', fullNameAr: 'أ', fullNameEn: 'A',
      department: null, title: null, points: 40, level: 2, roles: ['submitter'],
    });
    const result = await promise;
    expect(result.points).toBe(40);
    expect(result.level).toBe(2);
  });

  it('gets badges', async () => {
    const promise = service.getBadges();
    const req = httpMock.expectOne('/api/me/badges');
    req.flush({ badges: [{ code: 'first-idea', nameAr: 'أ', nameEn: 'First Idea', descriptionAr: null, descriptionEn: null, iconUrl: null, earnedAt: '2026-06-01T00:00:00Z' }] });
    const result = await promise;
    expect(result.badges.length).toBe(1);
    expect(result.badges[0].earnedAt).toBe('2026-06-01T00:00:00Z');
  });
});

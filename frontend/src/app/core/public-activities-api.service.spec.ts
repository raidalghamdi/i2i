import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PublicActivitiesApiService } from './public-activities-api.service';

describe('PublicActivitiesApiService', () => {
  let service: PublicActivitiesApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(PublicActivitiesApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });
  afterEach(() => httpMock.verify());

  it('lists activities', async () => {
    const promise = service.list();
    const req = httpMock.expectOne('/api/public/activities');
    expect(req.request.method).toBe('GET');
    req.flush([
      {
        id: '1',
        nameAr: 'أ',
        nameEn: 'Activity A',
        type: 'workshop',
        status: 'active',
        startDate: '2026-01-01',
        endDate: '2026-01-31',
        ideaCount: 3,
      },
    ]);
    const result = await promise;
    expect(result.length).toBe(1);
    expect(result[0].nameEn).toBe('Activity A');
  });

  it('gets activity detail by id', async () => {
    const promise = service.getById('1');
    const req = httpMock.expectOne('/api/public/activities/1');
    expect(req.request.method).toBe('GET');
    req.flush({
      activity: {
        id: '1',
        nameAr: 'أ',
        nameEn: 'Activity A',
        type: 'workshop',
        status: 'active',
        startDate: '2026-01-01',
        endDate: '2026-01-31',
        ideaCount: 3,
      },
      approvedCount: 2,
      pilotingCount: 1,
      ideas: [{ id: 'i1', code: 'IDEA-1', titleAr: 'فكرة', titleEn: 'Idea', status: 'approved' }],
    });
    const result = await promise;
    expect(result?.activity.nameEn).toBe('Activity A');
    expect(result?.approvedCount).toBe(2);
    expect(result?.ideas.length).toBe(1);
  });

  it('returns null on 404', async () => {
    const promise = service.getById('missing');
    httpMock.expectOne('/api/public/activities/missing').flush('', { status: 404, statusText: 'Not Found' });
    expect(await promise).toBeNull();
  });
});

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ActivitiesService } from './activities.service';

describe('ActivitiesService', () => {
  let service: ActivitiesService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ActivitiesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list() gets /api/activities', async () => {
    const promise = service.list();
    const req = httpMock.expectOne('/api/activities');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'activity-1', nameAr: 'أ', nameEn: 'Activity One' }]);

    expect(await promise).toEqual([{ id: 'activity-1', nameAr: 'أ', nameEn: 'Activity One' }]);
  });
});

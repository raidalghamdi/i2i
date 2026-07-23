import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PublicTracksApiService } from './public-tracks-api.service';

describe('PublicTracksApiService', () => {
  let service: PublicTracksApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(PublicTracksApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });
  afterEach(() => httpMock.verify());

  it('lists tracks', async () => {
    const promise = service.list();
    const req = httpMock.expectOne('/api/public/tracks');
    expect(req.request.method).toBe('GET');
    req.flush([
      { id: '1', nameAr: 'أ', nameEn: 'Track A', descriptionAr: 'وصف', descriptionEn: 'Desc', priority: 1 },
    ]);
    const result = await promise;
    expect(result.length).toBe(1);
    expect(result[0].nameEn).toBe('Track A');
  });

  it('gets track detail by id', async () => {
    const promise = service.getById('1');
    const req = httpMock.expectOne('/api/public/tracks/1');
    expect(req.request.method).toBe('GET');
    req.flush({
      track: { id: '1', nameAr: 'أ', nameEn: 'Track A', descriptionAr: 'وصف', descriptionEn: 'Desc', priority: 1 },
      challenges: [{ id: 'c1', textAr: 'تحدي', textEn: 'Challenge' }],
      ideas: [{ id: 'i1', code: 'IDEA-1', titleAr: 'فكرة', titleEn: 'Idea', status: 'submitted' }],
    });
    const result = await promise;
    expect(result?.track.nameEn).toBe('Track A');
    expect(result?.challenges.length).toBe(1);
    expect(result?.ideas.length).toBe(1);
  });

  it('returns null on 404', async () => {
    const promise = service.getById('missing');
    httpMock.expectOne('/api/public/tracks/missing').flush('', { status: 404, statusText: 'Not Found' });
    expect(await promise).toBeNull();
  });
});

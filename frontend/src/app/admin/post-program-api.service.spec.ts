import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PostProgramApiService } from './post-program-api.service';
import { PostProgramIdea } from './post-program.model';

describe('PostProgramApiService', () => {
  let service: PostProgramApiService;
  let httpMock: HttpTestingController;
  const sample: PostProgramIdea = { id: 'i1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'Idea', status: 'approved' };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(PostProgramApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });
  afterEach(() => httpMock.verify());

  it('lists post-program ideas', async () => {
    const promise = service.getIdeas();
    const req = httpMock.expectOne('/api/admin/post-program/ideas');
    expect(req.request.method).toBe('GET');
    req.flush([sample]);
    expect(await promise).toEqual([sample]);
  });

  it('advances an idea to a stage', async () => {
    const promise = service.advance('i1', 'in_pilot');
    const req = httpMock.expectOne('/api/admin/ideas/i1/post-program-stage');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ stage: 'in_pilot' });
    req.flush({ id: 'i1', status: 'in_pilot' });
    expect(await promise).toEqual({ id: 'i1', status: 'in_pilot' });
  });
});

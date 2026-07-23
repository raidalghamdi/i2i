import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { EvaluationSettingsApiService } from './evaluation-settings-api.service';
import { EvaluationSettings } from './evaluation-settings.model';

describe('EvaluationSettingsApiService', () => {
  let service: EvaluationSettingsApiService;
  let httpMock: HttpTestingController;

  const sample: EvaluationSettings = { passThreshold: 6, updatedAt: '2026-07-21T00:00:00Z' };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(EvaluationSettingsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('gets the settings', async () => {
    const promise = service.get();
    const req = httpMock.expectOne('/api/admin/evaluation/settings');
    expect(req.request.method).toBe('GET');
    req.flush(sample);
    expect(await promise).toEqual(sample);
  });

  it('patches the settings', async () => {
    const promise = service.update({ passThreshold: 7.5 });
    const req = httpMock.expectOne('/api/admin/evaluation/settings');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ passThreshold: 7.5 });
    req.flush({ ...sample, passThreshold: 7.5 });
    expect((await promise).passThreshold).toBe(7.5);
  });
});

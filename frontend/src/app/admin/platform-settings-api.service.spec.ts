import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PlatformSettingsApiService } from './platform-settings-api.service';

describe('PlatformSettingsApiService', () => {
  let service: PlatformSettingsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(PlatformSettingsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists settings via GET /api/admin/settings', async () => {
    const promise = service.list();
    const req = httpMock.expectOne('/api/admin/settings');
    expect(req.request.method).toBe('GET');
    req.flush([]);
    await expectAsync(promise).toBeResolvedTo([]);
  });

  it('patches a setting via PATCH /api/admin/settings/:key with { valueJson }', async () => {
    const promise = service.patch('site.title', '"My Site"');
    const req = httpMock.expectOne('/api/admin/settings/site.title');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ valueJson: '"My Site"' });
    req.flush({ key: 'site.title', valueJson: '"My Site"', updatedAt: '2026-01-01' });
    await promise;
  });
});

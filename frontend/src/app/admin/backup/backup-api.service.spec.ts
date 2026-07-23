import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { BackupApiService } from './backup-api.service';

describe('BackupApiService', () => {
  let service: BackupApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [BackupApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(BackupApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('requests the backup export as a blob', async () => {
    const blob = new Blob(['xlsx']);
    const promise = service.downloadBackup();
    const req = httpMock.expectOne('/api/admin/backup/export');
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(blob);
    await expectAsync(promise).toBeResolvedTo(blob);
  });
});

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { NotificationsApiService } from './notifications-api.service';

describe('NotificationsApiService', () => {
  let service: NotificationsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(NotificationsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists notifications', async () => {
    const promise = service.list();
    httpMock.expectOne('/api/notifications').flush([
      { id: 'n1', notificationType: 'idea_status', titleAr: 'ت', titleEn: 'T', bodyAr: 'ب', bodyEn: 'B', link: null, readAt: null, createdAt: '2026-07-01T00:00:00Z' },
    ]);
    const result = await promise;
    expect(result.length).toBe(1);
  });

  it('marks one notification read', async () => {
    const promise = service.markRead('n1');
    const req = httpMock.expectOne('/api/notifications/n1/read');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'n1', notificationType: 'idea_status', titleAr: 'ت', titleEn: 'T', bodyAr: 'ب', bodyEn: 'B', link: null, readAt: '2026-07-02T00:00:00Z', createdAt: '2026-07-01T00:00:00Z' });
    const result = await promise;
    expect(result.readAt).toBe('2026-07-02T00:00:00Z');
  });

  it('marks all notifications read', async () => {
    const promise = service.markAllRead();
    const req = httpMock.expectOne('/api/notifications/read-all');
    expect(req.request.method).toBe('POST');
    req.flush({ markedCount: 3 });
    const result = await promise;
    expect(result.markedCount).toBe(3);
  });
});

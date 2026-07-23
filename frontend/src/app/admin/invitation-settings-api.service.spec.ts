import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { InvitationSettingsApiService } from './invitation-settings-api.service';
import { InvitationReminderSettings } from './invitation-settings.model';

describe('InvitationSettingsApiService', () => {
  let service: InvitationSettingsApiService;
  let httpMock: HttpTestingController;

  const sample: InvitationReminderSettings = {
    enabled: true,
    cronExpression: '0 9 * * 1',
    timezone: 'Asia/Riyadh',
    stopAfterNReminders: 3,
    gapHours: 48,
    expiresDays: 14,
    fromName: 'Innovation-to-Impact Program',
    fromEmail: 'noreply@gac.gov.sa',
    programNameAr: 'برنامج ابتكر لمنافس',
    programNameEn: 'Innovation-to-Impact Program',
    updatedAt: '2026-07-19T00:00:00Z',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(InvitationSettingsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('gets the settings', async () => {
    const promise = service.get();
    httpMock.expectOne('/api/admin/invitations/settings').flush(sample);
    expect(await promise).toEqual(sample);
  });

  it('patches the settings', async () => {
    const promise = service.update({ enabled: false, gapHours: 72 });
    const req = httpMock.expectOne('/api/admin/invitations/settings');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ enabled: false, gapHours: 72 });
    req.flush({ ...sample, enabled: false, gapHours: 72 });
    expect((await promise).gapHours).toBe(72);
  });
});

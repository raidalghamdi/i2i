import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { RosterApiService } from './roster-api.service';

describe('RosterApiService', () => {
  let service: RosterApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(RosterApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('gets the hub via GET /api/admin/roster', async () => {
    const promise = service.getHub();
    const req = httpMock.expectOne('/api/admin/roster');
    expect(req.request.method).toBe('GET');
    req.flush([]);
    await expectAsync(promise).toBeResolvedTo([]);
  });

  it('gets role detail via GET /api/admin/roster/:roleCode', async () => {
    const promise = service.getRoleDetail('evaluator');
    const req = httpMock.expectOne('/api/admin/roster/evaluator');
    expect(req.request.method).toBe('GET');
    const detail = {
      roleCode: 'evaluator',
      roleNameAr: 'مقيّم',
      roleNameEn: 'Evaluator',
      activeMembers: [],
      invitations: [],
    };
    req.flush(detail);
    await expectAsync(promise).toBeResolvedTo(detail);
  });

  it('invites via POST /api/admin/roster/:roleCode/invite', async () => {
    const promise = service.invite('evaluator', ['jdoe', 'asmith'], '2026-08-01T00:00:00Z');
    const req = httpMock.expectOne('/api/admin/roster/evaluator/invite');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ samAccountNames: ['jdoe', 'asmith'], deadlineAt: '2026-08-01T00:00:00Z' });
    const result = { total: 2, created: 2, skipped: 0, errors: [] };
    req.flush(result);
    await expectAsync(promise).toBeResolvedTo(result);
  });

  it('withdraws via POST /api/admin/roster/:id/withdraw', async () => {
    const promise = service.withdraw('inv1');
    const req = httpMock.expectOne('/api/admin/roster/inv1/withdraw');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    const result = { id: 'inv1', status: 'withdrawn' };
    req.flush(result);
    await expectAsync(promise).toBeResolvedTo(result);
  });

  it('bulk withdraws via POST /api/admin/roster/withdraw-bulk', async () => {
    const promise = service.bulkWithdraw(['inv1', 'inv2']);
    const req = httpMock.expectOne('/api/admin/roster/withdraw-bulk');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ ids: ['inv1', 'inv2'] });
    const result = { withdrawn: 2 };
    req.flush(result);
    await expectAsync(promise).toBeResolvedTo(result);
  });

  it('reminds via POST /api/admin/roster/:id/remind', async () => {
    const promise = service.remind('inv1');
    const req = httpMock.expectOne('/api/admin/roster/inv1/remind');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    const result = { id: 'inv1', reminderCount: 1 };
    req.flush(result);
    await expectAsync(promise).toBeResolvedTo(result);
  });

  it('bulk reminds via POST /api/admin/roster/remind-bulk', async () => {
    const promise = service.bulkRemind(['inv1', 'inv2']);
    const req = httpMock.expectOne('/api/admin/roster/remind-bulk');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ ids: ['inv1', 'inv2'] });
    const result = { reminded: 2 };
    req.flush(result);
    await expectAsync(promise).toBeResolvedTo(result);
  });

  it('gets settings via GET /api/admin/roster/settings', async () => {
    const promise = service.getSettings();
    const req = httpMock.expectOne('/api/admin/roster/settings');
    expect(req.request.method).toBe('GET');
    const settings = {
      enabled: true,
      defaultExpiresDays: 14,
      reminderGapHours: 48,
      maxReminders: 3,
      updatedAt: '2026-07-01T00:00:00Z',
    };
    req.flush(settings);
    await expectAsync(promise).toBeResolvedTo(settings);
  });

  it('updates settings via PATCH /api/admin/roster/settings', async () => {
    const promise = service.updateSettings({ enabled: false, maxReminders: 5 });
    const req = httpMock.expectOne('/api/admin/roster/settings');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ enabled: false, maxReminders: 5 });
    const settings = {
      enabled: false,
      defaultExpiresDays: 14,
      reminderGapHours: 48,
      maxReminders: 5,
      updatedAt: '2026-07-16T00:00:00Z',
    };
    req.flush(settings);
    await expectAsync(promise).toBeResolvedTo(settings);
  });

  it('imports employees via POST /api/admin/employees/import', async () => {
    const rows = [{ samAccountName: 'jdoe', roleCode: 'evaluator' }];
    const promise = service.importEmployees(rows);
    const req = httpMock.expectOne('/api/admin/employees/import');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ rows });
    const result = { total: 1, created: 1, skipped: 0, errors: [] };
    req.flush(result);
    await expectAsync(promise).toBeResolvedTo(result);
  });
});

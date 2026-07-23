import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { DashboardApiService } from './dashboard-api.service';

describe('DashboardApiService', () => {
  let service: DashboardApiService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [DashboardApiService, provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(DashboardApiService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('GETs /api/dashboard/admin', async () => {
    const p = service.getAdmin();
    const req = http.expectOne('/api/dashboard/admin');
    expect(req.request.method).toBe('GET');
    req.flush({ totalUsers: 3, activeIdeas: 2, pendingEvaluations: 1, health: 'Healthy' });
    expect((await p).totalUsers).toBe(3);
  });

  it('GETs /api/dashboard/committee and /supervisor', async () => {
    const c = service.getCommittee();
    http.expectOne('/api/dashboard/committee').flush({ awaitingDecision: 1, decisionsThisWeek: 2 });
    expect((await c).awaitingDecision).toBe(1);
    const s = service.getSupervisor();
    http.expectOne('/api/dashboard/supervisor').flush({ teamMembers: 5, sectorIdeas: 4, escalationsAwaitingMe: 1, screening: { total: 3, underReview: 1, approved: 1, returned: 0, rejected: 1 } });
    expect((await s).screening.approved).toBe(1);
  });
});

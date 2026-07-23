import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PendingApproval } from './approval.model';
import { ApprovalsApiService } from './approvals-api.service';

describe('ApprovalsApiService', () => {
  let service: ApprovalsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ApprovalsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists pending approvals via GET /api/approvals and maps .items', async () => {
    const items: PendingApproval[] = [
      {
        instanceId: 'i1',
        stepId: 's1',
        entityType: 'idea',
        entityId: 'e1',
        chainNameAr: 'سلسلة',
        chainNameEn: 'chain',
        stepLabelAr: 'خطوة',
        stepLabelEn: 'step',
        stepOrder: 1,
        minApprovers: 1,
        priorApprovers: 0,
      },
    ];
    const promise = service.list();
    const req = httpMock.expectOne('/api/approvals');
    expect(req.request.method).toBe('GET');
    req.flush({ items });
    await expectAsync(promise).toBeResolvedTo(items);
  });

  it('decides via POST /api/approvals/decide with the right body', async () => {
    const promise = service.decide('i1', 's1', 'approve', 'looks good');
    const req = httpMock.expectOne('/api/approvals/decide');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      instanceId: 'i1',
      stepId: 's1',
      decision: 'approve',
      comment: 'looks good',
    });
    req.flush(null);
    await promise;
  });

  it('bulk-decides via POST /api/approvals/bulk-decide and returns the result', async () => {
    const targets = [{ instanceId: 'i1', stepId: 's1' }, { instanceId: 'i2', stepId: 's2' }];
    const promise = service.bulkDecide(targets, 'reject', 'not ready');
    const req = httpMock.expectOne('/api/approvals/bulk-decide');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ targets, decision: 'reject', comment: 'not ready' });
    req.flush({ succeeded: 1, failed: ['i2'] });
    await expectAsync(promise).toBeResolvedTo({ succeeded: 1, failed: ['i2'] });
  });
});

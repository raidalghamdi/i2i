import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuditApiService } from '../audit-api.service';
import { AuditRow } from '../audit.model';
import { ReportsApiService } from '../reports-api.service';
import { AuditBrowseComponent } from './audit-browse.component';

describe('AuditBrowseComponent', () => {
  let fixture: ComponentFixture<AuditBrowseComponent>;
  let auditApi: jasmine.SpyObj<AuditApiService>;
  let reportsApi: jasmine.SpyObj<ReportsApiService>;

  const verifiedRow: AuditRow = {
    id: 'a1',
    chainSeq: 1,
    occurredAt: '2026-01-01T10:00:00Z',
    actorName: 'Alice',
    entityType: 'idea',
    entityId: 'idea-1',
    action: 'create',
    verified: true,
  };

  const unverifiedRow: AuditRow = {
    id: 'a2',
    chainSeq: 2,
    occurredAt: '2026-01-02T11:00:00Z',
    actorName: null,
    entityType: 'api_request',
    entityId: 'req-1',
    action: 'update',
    verified: false,
  };

  function setup(rows: AuditRow[]): void {
    auditApi = jasmine.createSpyObj('AuditApiService', ['browse']);
    auditApi.browse.and.returnValue(Promise.resolve({ items: rows, total: rows.length, page: 1, pageSize: 25 }));

    reportsApi = jasmine.createSpyObj('ReportsApiService', ['generateAuditLogReport', 'downloadReport']);

    TestBed.configureTestingModule({
      imports: [AuditBrowseComponent],
      providers: [
        provideRouter([]),
        { provide: AuditApiService, useValue: auditApi },
        { provide: ReportsApiService, useValue: reportsApi },
      ],
    });
    fixture = TestBed.createComponent(AuditBrowseComponent);
  }

  it('renders one row per audit entry', async () => {
    setup([verifiedRow, unverifiedRow]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Alice');
    expect(fixture.nativeElement.textContent).toContain('create');
    expect(fixture.nativeElement.textContent).toContain('update');
  });

  it('shows an empty-state message when there are no audit entries', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No audit entries');
  });

  it('renders a verified badge for verified rows and an unverified badge otherwise', async () => {
    setup([verifiedRow, unverifiedRow]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const verifiedBadges = fixture.nativeElement.querySelectorAll('.audit-verified');
    const unverifiedBadges = fixture.nativeElement.querySelectorAll('.audit-unverified');
    expect(verifiedBadges.length).toBe(1);
    expect(unverifiedBadges.length).toBe(1);
  });

  it('re-queries the audit log with the selected filters when a filter changes', async () => {
    setup([verifiedRow]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    auditApi.browse.calls.reset();
    fixture.componentInstance.entityType.set('idea');
    await fixture.componentInstance.onFilterChange();

    expect(auditApi.browse).toHaveBeenCalledWith({
      entityType: 'idea',
      action: undefined,
      actorId: undefined,
      from: undefined,
      to: undefined,
      page: 1,
      pageSize: 25,
    });
  });

  it('resets to page 1 when a filter changes after paging forward', async () => {
    setup([verifiedRow]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    fixture.componentInstance.page.set(3);
    fixture.componentInstance.action.set('update');
    await fixture.componentInstance.onFilterChange();

    expect(fixture.componentInstance.page()).toBe(1);
  });

  it('advances to the next page and reloads', async () => {
    auditApi = jasmine.createSpyObj('AuditApiService', ['browse']);
    auditApi.browse.and.returnValue(Promise.resolve({ items: [verifiedRow], total: 50, page: 1, pageSize: 25 }));
    reportsApi = jasmine.createSpyObj('ReportsApiService', ['generateAuditLogReport', 'downloadReport']);
    TestBed.configureTestingModule({
      imports: [AuditBrowseComponent],
      providers: [
        provideRouter([]),
        { provide: AuditApiService, useValue: auditApi },
        { provide: ReportsApiService, useValue: reportsApi },
      ],
    });
    fixture = TestBed.createComponent(AuditBrowseComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    auditApi.browse.calls.reset();
    await fixture.componentInstance.onNext();

    expect(fixture.componentInstance.page()).toBe(2);
    expect(auditApi.browse).toHaveBeenCalledWith(jasmine.objectContaining({ page: 2 }));
  });

  it('exports the audit log by generating then downloading the report', async () => {
    setup([verifiedRow]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    reportsApi.generateAuditLogReport.and.returnValue(
      Promise.resolve({ reportGenerationId: 'r1', status: 'completed', fileUrl: '/tmp/audit-log.xlsx' }),
    );
    reportsApi.downloadReport.and.returnValue(Promise.resolve(new Blob(['data'])));

    await fixture.componentInstance.onExport();

    expect(reportsApi.generateAuditLogReport).toHaveBeenCalled();
    expect(reportsApi.downloadReport).toHaveBeenCalledWith('r1');
    expect(fixture.componentInstance.errorMessage()).toBeNull();
  });

  it('shows an error and does not download when export generation status is failed', async () => {
    setup([verifiedRow]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    reportsApi.generateAuditLogReport.and.returnValue(
      Promise.resolve({ reportGenerationId: 'r2', status: 'failed', fileUrl: null }),
    );

    await fixture.componentInstance.onExport();

    expect(reportsApi.downloadReport).not.toHaveBeenCalled();
    expect(fixture.componentInstance.errorMessage()).toBe('Report generation failed. Please try again.');
  });

  it('shows an error state with retry when the browse call fails, and recovers on retry', async () => {
    auditApi = jasmine.createSpyObj('AuditApiService', ['browse']);
    auditApi.browse.and.returnValue(Promise.reject(new Error('boom')));
    reportsApi = jasmine.createSpyObj('ReportsApiService', ['generateAuditLogReport', 'downloadReport']);
    TestBed.configureTestingModule({
      imports: [AuditBrowseComponent],
      providers: [
        provideRouter([]),
        { provide: AuditApiService, useValue: auditApi },
        { provide: ReportsApiService, useValue: reportsApi },
      ],
    });
    fixture = TestBed.createComponent(AuditBrowseComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeTruthy();
    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    expect(retryButton).toBeTruthy();

    auditApi.browse.and.returnValue(Promise.resolve({ items: [verifiedRow], total: 1, page: 1, pageSize: 25 }));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Alice');
  });
});

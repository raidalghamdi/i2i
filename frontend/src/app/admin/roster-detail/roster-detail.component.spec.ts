import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { RosterApiService } from '../roster-api.service';
import { BulkCreateResult, RoleInvitation, RosterRoleDetail } from '../roster.model';
import { RosterDetailComponent } from './roster-detail.component';

describe('RosterDetailComponent', () => {
  let fixture: ComponentFixture<RosterDetailComponent>;
  let api: jasmine.SpyObj<RosterApiService>;

  const pendingInvitation: RoleInvitation = {
    id: 'inv-1',
    samAccountName: 'abrown',
    displayName: null,
    email: null,
    status: 'pending',
    deadlineAt: null,
    respondedAt: null,
    reminderCount: 0,
    lastReminderAt: null,
    source: 'manual',
    invitedByName: 'Admin One',
    createdAt: '2026-07-01T00:00:00Z',
  };

  const withdrawnInvitation: RoleInvitation = {
    ...pendingInvitation,
    id: 'inv-2',
    samAccountName: 'cdavis',
    status: 'withdrawn',
  };

  const detail: RosterRoleDetail = {
    roleCode: 'evaluator',
    roleNameAr: 'مقيّم',
    roleNameEn: 'Evaluator',
    activeMembers: [
      { userId: 'u-1', samAccountName: 'jsmith', fullNameAr: 'ا', fullNameEn: 'J Smith', email: 'jsmith@gac-demo.sa', isActive: true },
    ],
    invitations: [pendingInvitation, withdrawnInvitation],
  };

  function setup(): void {
    api = jasmine.createSpyObj('RosterApiService', ['getRoleDetail', 'invite', 'withdraw', 'bulkWithdraw', 'remind', 'bulkRemind']);
    api.getRoleDetail.and.returnValue(Promise.resolve(detail));

    TestBed.configureTestingModule({
      imports: [RosterDetailComponent],
      providers: [
        { provide: RosterApiService, useValue: api },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'evaluator' } } } },
      ],
    });
    fixture = TestBed.createComponent(RosterDetailComponent);
  }

  it('loads active members and invitations for the route roleCode exactly once on init', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(api.getRoleDetail).toHaveBeenCalledWith('evaluator');
    expect(api.getRoleDetail).toHaveBeenCalledTimes(1);
    expect(fixture.nativeElement.textContent).toContain('J Smith');
    expect(fixture.nativeElement.textContent).toContain('abrown');
  });

  it('shows an error state with retry when the role detail fails to load, and recovers on retry', async () => {
    api = jasmine.createSpyObj('RosterApiService', ['getRoleDetail', 'invite', 'withdraw', 'bulkWithdraw', 'remind', 'bulkRemind']);
    api.getRoleDetail.and.returnValue(Promise.reject({ error: { error: 'Role detail unavailable' } }));

    TestBed.configureTestingModule({
      imports: [RosterDetailComponent],
      providers: [
        { provide: RosterApiService, useValue: api },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'evaluator' } } } },
      ],
    });
    fixture = TestBed.createComponent(RosterDetailComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Role detail unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    api.getRoleDetail.and.returnValue(Promise.resolve(detail));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('J Smith');
  });

  it('does not re-fetch when ngOnInit is invoked a second time (Angular double-invoke guard)', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    await fixture.componentInstance.ngOnInit();

    expect(api.getRoleDetail).toHaveBeenCalledTimes(1);
  });

  it('only enables withdraw/remind buttons for pending-status invitation rows', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const rows = Array.from(fixture.nativeElement.querySelectorAll('tbody')[1].querySelectorAll('tr')) as HTMLTableRowElement[];
    const pendingRow = rows.find((r) => r.textContent?.includes('abrown'))!;
    const withdrawnRow = rows.find((r) => r.textContent?.includes('cdavis'))!;

    const pendingButtons = Array.from(pendingRow.querySelectorAll('button')) as HTMLButtonElement[];
    const withdrawnButtons = Array.from(withdrawnRow.querySelectorAll('button')) as HTMLButtonElement[];

    expect(pendingButtons.every((b) => !b.disabled)).toBe(true);
    expect(withdrawnButtons.every((b) => b.disabled)).toBe(true);
  });

  it('submits comma/newline-separated names with an optional deadline, calls invite, and reloads', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    const result: BulkCreateResult = { total: 2, created: 2, skipped: 0, errors: [] };
    api.invite.and.returnValue(Promise.resolve(result));

    fixture.componentInstance.inviteText.set('abrown,\ncdavis');
    fixture.componentInstance.deadlineAt.set('2026-08-01');
    await fixture.componentInstance.onInvite();

    expect(api.invite).toHaveBeenCalledWith('evaluator', ['abrown', 'cdavis'], '2026-08-01');
    expect(api.getRoleDetail).toHaveBeenCalledTimes(2);
    expect(fixture.componentInstance.inviteText()).toBe('');
    expect(fixture.componentInstance.deadlineAt()).toBe('');
  });

  it('submits semicolon-separated names (mixed with comma/newline) by splitting on all three separators', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    const result: BulkCreateResult = { total: 3, created: 3, skipped: 0, errors: [] };
    api.invite.and.returnValue(Promise.resolve(result));

    fixture.componentInstance.inviteText.set('abrown, cdavis;jsmith\nesmith');
    await fixture.componentInstance.onInvite();

    expect(api.invite).toHaveBeenCalledWith('evaluator', ['abrown', 'cdavis', 'jsmith', 'esmith'], null);
    expect(api.getRoleDetail).toHaveBeenCalledTimes(2);
  });

  it('surfaces BulkCreateResult errors in the UI after inviting', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    const result: BulkCreateResult = {
      total: 1,
      created: 0,
      skipped: 1,
      errors: [{ samAccountName: 'baduser', message: 'Not found in AD' }],
    };
    api.invite.and.returnValue(Promise.resolve(result));

    fixture.componentInstance.inviteText.set('baduser');
    await fixture.componentInstance.onInvite();
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toContain('baduser');
    expect(fixture.componentInstance.errorMessage()).toContain('Not found in AD');
    expect(fixture.nativeElement.textContent).toContain('Not found in AD');
  });

  it('withdraws a single pending row and refreshes', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    api.withdraw.and.returnValue(Promise.resolve({ id: 'inv-1', status: 'withdrawn' }));
    await fixture.componentInstance.onWithdraw('inv-1');

    expect(api.withdraw).toHaveBeenCalledWith('inv-1');
    expect(api.getRoleDetail).toHaveBeenCalledTimes(2);
  });

  it('reminds a single pending row and refreshes', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    api.remind.and.returnValue(Promise.resolve({ id: 'inv-1', reminderCount: 1 }));
    await fixture.componentInstance.onRemind('inv-1');

    expect(api.remind).toHaveBeenCalledWith('inv-1');
    expect(api.getRoleDetail).toHaveBeenCalledTimes(2);
  });

  it('surfaces a 409 backend error from remind without pre-computing eligibility client-side', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    api.remind.and.returnValue(Promise.reject({ status: 409, error: { error: 'Reminder cap reached' } }));
    await fixture.componentInstance.onRemind('inv-1');

    expect(fixture.componentInstance.errorMessage()).toBe('Reminder cap reached');
    expect(api.getRoleDetail).toHaveBeenCalledTimes(1);
  });

  it('bulk-withdraws selected rows, clears selection, and refreshes', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    fixture.componentInstance.toggleSelected('inv-1');
    api.bulkWithdraw.and.returnValue(Promise.resolve({ withdrawn: 1 }));
    await fixture.componentInstance.onBulkWithdraw();

    expect(api.bulkWithdraw).toHaveBeenCalledWith(['inv-1']);
    expect(fixture.componentInstance.selectedIds().size).toBe(0);
    expect(api.getRoleDetail).toHaveBeenCalledTimes(2);
  });

  it('bulk-reminds selected rows, clears selection, and refreshes', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    fixture.componentInstance.toggleSelected('inv-1');
    api.bulkRemind.and.returnValue(Promise.resolve({ reminded: 1 }));
    await fixture.componentInstance.onBulkRemind();

    expect(api.bulkRemind).toHaveBeenCalledWith(['inv-1']);
    expect(fixture.componentInstance.selectedIds().size).toBe(0);
    expect(api.getRoleDetail).toHaveBeenCalledTimes(2);
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AdminApiService } from '../admin-api.service';
import { AdminUser, PendingRoleGrant } from '../admin.model';
import { UserListComponent } from './user-list.component';

describe('UserListComponent', () => {
  let fixture: ComponentFixture<UserListComponent>;
  let adminApi: jasmine.SpyObj<AdminApiService>;

  function setup(users: AdminUser[], pendingGrants: PendingRoleGrant[]): void {
    adminApi = jasmine.createSpyObj('AdminApiService', ['listUsers', 'listPendingGrants', 'cancelPendingGrant', 'listRoles', 'grantRole']);
    adminApi.listUsers.and.returnValue(Promise.resolve(users));
    adminApi.listPendingGrants.and.returnValue(Promise.resolve(pendingGrants));
    adminApi.listRoles.and.returnValue(Promise.resolve([{ id: 'role-1', code: 'evaluator', nameEn: 'Evaluator' }]));

    TestBed.configureTestingModule({
      imports: [UserListComponent],
      providers: [provideRouter([]), { provide: AdminApiService, useValue: adminApi }],
    });
    fixture = TestBed.createComponent(UserListComponent);
  }

  it('renders one row per user with roles and active status', async () => {
    setup(
      [{ id: 'user-1', samAccountName: 'jsmith', email: 'jsmith@gac-demo.sa', fullNameAr: 'ا', fullNameEn: 'J Smith', department: 'Innovation', title: 'Analyst', isActive: true, roles: [{ roleId: 'role-1', code: 'evaluator', nameEn: 'Evaluator' }] }],
      [],
    );
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('J Smith');
    expect(fixture.nativeElement.textContent).toContain('Evaluator');
  });

  it('renders pending grants and cancels one', async () => {
    setup([], [{ id: 'grant-1', samAccountName: 'futureuser1', roleCode: 'evaluator', roleNameEn: 'Evaluator', grantedByName: 'Admin One', grantedAt: '2026-01-01' }]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('futureuser1');

    adminApi.cancelPendingGrant.and.returnValue(Promise.resolve());
    adminApi.listPendingGrants.and.returnValue(Promise.resolve([]));

    await fixture.componentInstance.onCancelPendingGrant('grant-1');

    expect(adminApi.cancelPendingGrant).toHaveBeenCalledWith('grant-1');
    expect(fixture.componentInstance.pendingGrants().length).toBe(0);
  });

  it('shows an empty-state message when there are no users', async () => {
    setup([], []);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('no users');
  });

  it('grants a role by SAM account name and refreshes users and pending grants', async () => {
    setup([], []);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    adminApi.grantRole.and.returnValue(Promise.resolve({ status: 'pending', userId: null, pendingGrantId: 'grant-2' }));
    adminApi.listUsers.and.returnValue(Promise.resolve([]));
    adminApi.listPendingGrants.and.returnValue(Promise.resolve([{ id: 'grant-2', samAccountName: 'newperson1', roleCode: 'evaluator', roleNameEn: 'Evaluator', grantedByName: 'Admin One', grantedAt: '2026-01-01' }]));

    fixture.componentInstance.grantSamAccountName.set('newperson1');
    fixture.componentInstance.grantRoleCode.set('evaluator');
    await fixture.componentInstance.onGrantBySamAccountName();

    expect(adminApi.grantRole).toHaveBeenCalledWith({ samAccountName: 'newperson1', roleCode: 'evaluator' });
    expect(fixture.componentInstance.pendingGrants().length).toBe(1);
    expect(fixture.componentInstance.grantSamAccountName()).toBe('');
  });

  it('does not submit when the account name is empty', async () => {
    setup([], []);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    fixture.componentInstance.grantSamAccountName.set('');
    fixture.componentInstance.grantRoleCode.set('evaluator');
    await fixture.componentInstance.onGrantBySamAccountName();

    expect(adminApi.grantRole).not.toHaveBeenCalled();
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    adminApi = jasmine.createSpyObj('AdminApiService', ['listUsers', 'listPendingGrants', 'cancelPendingGrant', 'listRoles', 'grantRole']);
    adminApi.listUsers.and.returnValue(Promise.reject({ error: { error: 'Users unavailable' } }));
    adminApi.listPendingGrants.and.returnValue(Promise.resolve([]));
    adminApi.listRoles.and.returnValue(Promise.resolve([]));

    TestBed.configureTestingModule({
      imports: [UserListComponent],
      providers: [provideRouter([]), { provide: AdminApiService, useValue: adminApi }],
    });
    fixture = TestBed.createComponent(UserListComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Users unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    adminApi.listUsers.and.returnValue(Promise.resolve([{ id: 'user-1', samAccountName: 'jsmith', email: 'jsmith@gac-demo.sa', fullNameAr: 'ا', fullNameEn: 'J Smith', department: 'Innovation', title: 'Analyst', isActive: true, roles: [] }]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('J Smith');
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { AdminApiService } from '../admin-api.service';
import { AdminUser, RoleOption } from '../admin.model';
import { UserDetailComponent } from './user-detail.component';

describe('UserDetailComponent', () => {
  let fixture: ComponentFixture<UserDetailComponent>;
  let adminApi: jasmine.SpyObj<AdminApiService>;

  const baseUser: AdminUser = {
    id: 'user-1', samAccountName: 'jsmith', email: 'jsmith@gac-demo.sa', fullNameAr: 'ا', fullNameEn: 'J Smith',
    department: 'Innovation', title: 'Analyst', isActive: true, roles: [{ roleId: 'role-1', code: 'evaluator', nameEn: 'Evaluator' }],
  };
  const roleOptions: RoleOption[] = [
    { id: 'role-1', code: 'evaluator', nameEn: 'Evaluator' },
    { id: 'role-2', code: 'judge', nameEn: 'Judge' },
  ];

  function setup(user: AdminUser): void {
    adminApi = jasmine.createSpyObj('AdminApiService', ['getUser', 'listRoles', 'revokeRole', 'setActive']);
    adminApi.getUser.and.returnValue(Promise.resolve(user));
    adminApi.listRoles.and.returnValue(Promise.resolve(roleOptions));

    TestBed.configureTestingModule({
      imports: [UserDetailComponent],
      providers: [
        { provide: AdminApiService, useValue: adminApi },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'user-1' } } } },
      ],
    });
    fixture = TestBed.createComponent(UserDetailComponent);
  }

  it('renders read-only AD-synced fields and current roles', async () => {
    setup(baseUser);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Innovation');
    expect(fixture.nativeElement.textContent).toContain('Analyst');
    expect(fixture.nativeElement.textContent).toContain('Evaluator');
  });

  it('revokes a role and refreshes', async () => {
    setup(baseUser);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    adminApi.revokeRole.and.returnValue(Promise.resolve());
    adminApi.getUser.and.returnValue(Promise.resolve({ ...baseUser, roles: [] }));

    await fixture.componentInstance.onRevokeRole('role-1');

    expect(adminApi.revokeRole).toHaveBeenCalledWith('user-1', 'role-1');
    expect(fixture.componentInstance.user()?.roles.length).toBe(0);
  });

  it('toggles active status and refreshes', async () => {
    setup(baseUser);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    adminApi.setActive.and.returnValue(Promise.resolve());
    adminApi.getUser.and.returnValue(Promise.resolve({ ...baseUser, isActive: false }));

    await fixture.componentInstance.onToggleActive();

    expect(adminApi.setActive).toHaveBeenCalledWith('user-1', false);
    expect(fixture.componentInstance.user()?.isActive).toBe(false);
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    adminApi = jasmine.createSpyObj('AdminApiService', ['getUser', 'listRoles', 'revokeRole', 'setActive']);
    adminApi.getUser.and.returnValue(Promise.reject({ error: { error: 'User unavailable' } }));
    adminApi.listRoles.and.returnValue(Promise.resolve(roleOptions));

    TestBed.configureTestingModule({
      imports: [UserDetailComponent],
      providers: [
        { provide: AdminApiService, useValue: adminApi },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'user-1' } } } },
      ],
    });
    fixture = TestBed.createComponent(UserDetailComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('User unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    adminApi.getUser.and.returnValue(Promise.resolve(baseUser));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('J Smith');
  });
});

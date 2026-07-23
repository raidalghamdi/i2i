import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AdminApiService } from '../admin-api.service';
import { GroupGrantResult, RoleOption } from '../admin.model';
import { GroupGrantComponent } from './group-grant.component';

describe('GroupGrantComponent', () => {
  let fixture: ComponentFixture<GroupGrantComponent>;
  let adminApi: jasmine.SpyObj<AdminApiService>;

  const roleOptions: RoleOption[] = [{ id: 'role-1', code: 'evaluator', nameEn: 'Evaluator' }];

  function setup(): void {
    adminApi = jasmine.createSpyObj('AdminApiService', ['listRoles', 'grantRoleToGroup']);
    adminApi.listRoles.and.returnValue(Promise.resolve(roleOptions));

    TestBed.configureTestingModule({
      imports: [GroupGrantComponent],
      providers: [{ provide: AdminApiService, useValue: adminApi }],
    });
    fixture = TestBed.createComponent(GroupGrantComponent);
  }

  it('loads role options', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.roleOptions().length).toBe(1);
  });

  it('submits a group grant and shows the result summary', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const result: GroupGrantResult = { grantedCount: 2, pendingCount: 1, skippedCount: 0, errors: [] };
    adminApi.grantRoleToGroup.and.returnValue(Promise.resolve(result));

    fixture.componentInstance.groupName.set('GAC-Evaluators');
    fixture.componentInstance.selectedRoleCode.set('evaluator');
    await fixture.componentInstance.onSubmit();

    expect(adminApi.grantRoleToGroup).toHaveBeenCalledWith({ groupName: 'GAC-Evaluators', roleCode: 'evaluator' });
    expect(fixture.componentInstance.result()).toEqual(result);
  });

  it('does not submit when the group name is empty', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    fixture.componentInstance.groupName.set('');
    fixture.componentInstance.selectedRoleCode.set('evaluator');
    await fixture.componentInstance.onSubmit();

    expect(adminApi.grantRoleToGroup).not.toHaveBeenCalled();
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    adminApi = jasmine.createSpyObj('AdminApiService', ['listRoles', 'grantRoleToGroup']);
    adminApi.listRoles.and.returnValue(Promise.reject({ error: { error: 'Roles unavailable' } }));

    TestBed.configureTestingModule({
      imports: [GroupGrantComponent],
      providers: [{ provide: AdminApiService, useValue: adminApi }],
    });
    fixture = TestBed.createComponent(GroupGrantComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Roles unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    adminApi.listRoles.and.returnValue(Promise.resolve(roleOptions));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.componentInstance.roleOptions().length).toBe(1);
  });
});

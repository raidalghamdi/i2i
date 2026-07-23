import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RolesCatalogApiService } from '../roles-catalog-api.service';
import { RoleCatalogRow } from '../roles-catalog.model';
import { RolesCatalogComponent } from './roles-catalog.component';

describe('RolesCatalogComponent', () => {
  let fixture: ComponentFixture<RolesCatalogComponent>;
  let api: jasmine.SpyObj<RolesCatalogApiService>;

  const roleA: RoleCatalogRow = {
    id: 'role-1',
    code: 'innovator',
    nameAr: 'المبتكر',
    nameEn: 'Innovator',
    descriptionAr: null,
    descriptionEn: null,
    isSystem: false,
    isActive: true,
    sortOrder: 1,
  };

  const roleB: RoleCatalogRow = {
    id: 'role-2',
    code: 'admin',
    nameAr: 'المسؤول',
    nameEn: 'Admin',
    descriptionAr: null,
    descriptionEn: null,
    isSystem: true,
    isActive: true,
    sortOrder: 2,
  };

  async function setup(roles: RoleCatalogRow[]): Promise<void> {
    api = jasmine.createSpyObj('RolesCatalogApiService', ['list', 'patch']);
    api.list.and.returnValue(Promise.resolve(roles));
    api.patch.and.returnValue(Promise.resolve(roles[0]));

    await TestBed.configureTestingModule({
      imports: [RolesCatalogComponent],
      providers: [provideRouter([]), { provide: RolesCatalogApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(RolesCatalogComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('renders one row per role', async () => {
    await setup([roleA, roleB]);

    const codeCells = Array.from(
      fixture.nativeElement.querySelectorAll('tbody tr td:first-child'),
    ) as HTMLElement[];
    expect(codeCells.map((cell) => cell.textContent?.trim())).toEqual(['innovator', 'admin']);
  });

  it('shows an empty-state message when there are no roles', async () => {
    await setup([]);

    expect(fixture.nativeElement.textContent).toContain('No roles yet');
  });

  it('shows a lock/system marker only for the system role', async () => {
    await setup([roleA, roleB]);

    const rowsEls = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rowsEls[0].querySelector('[data-testid="role-system-lock"]')).toBeNull();
    expect(rowsEls[1].querySelector('[data-testid="role-system-lock"]')).not.toBeNull();
  });

  it('does not render any add or delete controls', () => {
    return setup([roleA, roleB]).then(() => {
      const text = (fixture.nativeElement.textContent ?? '') as string;
      expect(text).not.toContain('Add role');
      expect(text).not.toContain('Delete');

      const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
      expect(buttons.every((btn) => /save/i.test(btn.textContent ?? ''))).toBeTrue();
    });
  });

  it('saves an edited row via patch() with the editable fields and reloads', async () => {
    await setup([roleA, roleB]);

    const row = fixture.componentInstance.editableRows()[0];
    fixture.componentInstance.updateRow(row.id, { nameEn: 'Innovator (revised)', sortOrder: 5 });

    const updated = { ...roleA, nameEn: 'Innovator (revised)', sortOrder: 5 };
    api.patch.and.returnValue(Promise.resolve(updated));
    api.list.and.returnValue(Promise.resolve([updated, roleB]));

    await fixture.componentInstance.onSave(fixture.componentInstance.editableRows()[0]);

    expect(api.patch).toHaveBeenCalledWith('role-1', {
      nameAr: 'المبتكر',
      nameEn: 'Innovator (revised)',
      descriptionAr: null,
      descriptionEn: null,
      isActive: true,
      sortOrder: 5,
    });
    expect(api.list).toHaveBeenCalledTimes(2);
  });

  it('allows editing a system role (rename, reorder, toggle active) — it is not read-only', async () => {
    await setup([roleA, roleB]);

    const systemRow = fixture.componentInstance.editableRows()[1];
    expect(systemRow.isSystem).toBeTrue();

    fixture.componentInstance.updateRow(systemRow.id, { nameEn: 'Administrator', isActive: false });

    const updated = { ...roleB, nameEn: 'Administrator', isActive: false };
    api.patch.and.returnValue(Promise.resolve(updated));
    api.list.and.returnValue(Promise.resolve([roleA, updated]));

    await fixture.componentInstance.onSave(fixture.componentInstance.editableRows()[1]);

    expect(api.patch).toHaveBeenCalledWith('role-2', {
      nameAr: 'المسؤول',
      nameEn: 'Administrator',
      descriptionAr: null,
      descriptionEn: null,
      isActive: false,
      sortOrder: 2,
    });
  });

  it('surfaces an error message when save fails', async () => {
    await setup([roleA, roleB]);

    api.patch.and.returnValue(Promise.reject({ error: { error: 'Name is required.' } }));

    await fixture.componentInstance.onSave(fixture.componentInstance.editableRows()[0]);

    expect(fixture.componentInstance.errorMessage()).toBe('Name is required.');
  });

  it('shows an error state with retry when the list call fails, and recovers on retry', async () => {
    api = jasmine.createSpyObj('RolesCatalogApiService', ['list', 'patch']);
    api.list.and.returnValue(Promise.reject({ error: { error: 'boom' } }));

    await TestBed.configureTestingModule({
      imports: [RolesCatalogComponent],
      providers: [provideRouter([]), { provide: RolesCatalogApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(RolesCatalogComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('boom');
    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    expect(retryButton).toBeTruthy();

    api.list.and.returnValue(Promise.resolve([roleA]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('innovator');
  });
});

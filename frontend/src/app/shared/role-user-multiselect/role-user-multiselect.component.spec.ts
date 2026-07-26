import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoleUser } from '../../supervisor/supervisor.model';
import { RoleUserMultiselectComponent } from './role-user-multiselect.component';

describe('RoleUserMultiselectComponent', () => {
  let fixture: ComponentFixture<RoleUserMultiselectComponent>;

  const users: RoleUser[] = [
    { id: 'user-1', fullNameAr: 'أحمد علي', fullNameEn: 'Ahmed Ali' },
    { id: 'user-2', fullNameAr: 'سارة محمد', fullNameEn: 'Sara Mohammed' },
  ];

  function setup(selectedIds: string[] = []): void {
    TestBed.configureTestingModule({ imports: [RoleUserMultiselectComponent] });
    fixture = TestBed.createComponent(RoleUserMultiselectComponent);
    fixture.componentRef.setInput('users', users);
    fixture.componentRef.setInput('selectedIds', selectedIds);
    fixture.detectChanges();
  }

  it('renders one checkbox per user, labelled with the Arabic name', () => {
    setup();
    const checkboxes = fixture.nativeElement.querySelectorAll('input[type="checkbox"]');
    expect(checkboxes.length).toBe(2);
    expect(fixture.nativeElement.textContent).toContain('أحمد علي');
    expect(fixture.nativeElement.textContent).toContain('سارة محمد');
  });

  it('reflects the initial selection as checked', () => {
    setup(['user-2']);
    const checkboxes = fixture.nativeElement.querySelectorAll('input[type="checkbox"]') as NodeListOf<HTMLInputElement>;
    expect(checkboxes[0].checked).toBe(false);
    expect(checkboxes[1].checked).toBe(true);
  });

  it('emits the updated selection when a checkbox is toggled on, then off', () => {
    setup();
    const onChange = jasmine.createSpy('selectionChange');
    fixture.componentInstance.selectionChange.subscribe(onChange);

    const checkboxes = fixture.nativeElement.querySelectorAll('input[type="checkbox"]') as NodeListOf<HTMLInputElement>;
    checkboxes[0].dispatchEvent(new Event('change'));
    expect(onChange).toHaveBeenCalledWith(['user-1']);

    fixture.componentRef.setInput('selectedIds', ['user-1']);
    fixture.detectChanges();
    checkboxes[0].dispatchEvent(new Event('change'));
    expect(onChange).toHaveBeenCalledWith([]);
  });

  it('shows an empty message when there are no users', () => {
    setup();
    fixture.componentRef.setInput('users', []);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No users found for this role.');
  });
});

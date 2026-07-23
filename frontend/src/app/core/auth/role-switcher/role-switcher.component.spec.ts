import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { By } from '@angular/platform-browser';
import { IdentityService } from '../identity.service';
import { RoleSwitcherComponent } from './role-switcher.component';

describe('RoleSwitcherComponent', () => {
  let fixture: ComponentFixture<RoleSwitcherComponent>;
  let setActiveRoleSpy: jasmine.Spy;

  function setup(activeRole: string | null, roles: string[]) {
    setActiveRoleSpy = jasmine.createSpy('setActiveRole');
    TestBed.configureTestingModule({
      imports: [RoleSwitcherComponent],
      providers: [
        {
          provide: IdentityService,
          useValue: {
            identity: signal({ samAccountName: 'x', email: null, department: null, roles, activeRole }),
            setActiveRole: setActiveRoleSpy,
          },
        },
      ],
    });
    fixture = TestBed.createComponent(RoleSwitcherComponent);
    fixture.detectChanges();
  }

  it('renders one option per assigned role', () => {
    setup('submitter', ['submitter', 'evaluator']);
    const options = fixture.debugElement.queryAll(By.css('option'));
    expect(options.length).toBe(2);
  });

  it('shows a no-roles message when the user has zero roles', () => {
    setup(null, []);
    expect(fixture.nativeElement.querySelector('select')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('No roles assigned');
  });

  it('calls setActiveRole when the selection changes', () => {
    setup('submitter', ['submitter', 'evaluator']);
    const select: HTMLSelectElement = fixture.debugElement.query(By.css('select')).nativeElement;
    select.value = 'evaluator';
    select.dispatchEvent(new Event('change'));

    expect(setActiveRoleSpy).toHaveBeenCalledWith('evaluator');
  });
});

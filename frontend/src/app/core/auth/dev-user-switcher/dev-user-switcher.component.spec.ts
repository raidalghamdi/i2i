import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { environment } from '../../../../environments/environment';
import { DevUserSwitcherComponent } from './dev-user-switcher.component';

describe('DevUserSwitcherComponent', () => {
  const originalProduction = environment.production;
  const originalDevUser = environment.devUser;
  const originalDevUsers = environment.devUsers;

  function setup(): ComponentFixture<DevUserSwitcherComponent> {
    TestBed.configureTestingModule({ imports: [DevUserSwitcherComponent] });
    const fixture = TestBed.createComponent(DevUserSwitcherComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    localStorage.removeItem('devUser');
    (environment as { devUser: string }).devUser = 'devuser';
    (environment as { devUsers: string[] }).devUsers = ['devuser', 'judge3', 'admin1'];
  });

  afterEach(() => {
    (environment as { production: boolean }).production = originalProduction;
    (environment as { devUser: string }).devUser = originalDevUser;
    (environment as { devUsers: string[] }).devUsers = originalDevUsers;
    localStorage.removeItem('devUser');
  });

  it('renders one option per configured dev user when not in production', () => {
    (environment as { production: boolean }).production = false;
    const fixture = setup();
    const options = fixture.debugElement.queryAll(By.css('option'));
    expect(options.length).toBe(3);
  });

  it('renders nothing in production', () => {
    (environment as { production: boolean }).production = true;
    const fixture = setup();
    expect(fixture.nativeElement.querySelector('select')).toBeNull();
  });

  it('marks the localStorage override as the selected option', () => {
    (environment as { production: boolean }).production = false;
    localStorage.setItem('devUser', 'judge3');
    const fixture = setup();
    const select: HTMLSelectElement = fixture.debugElement.query(By.css('select')).nativeElement;
    expect(select.value).toBe('judge3');
  });

  it('prepends an off-list override so it still displays', () => {
    (environment as { production: boolean }).production = false;
    localStorage.setItem('devUser', 'someone-off-list');
    const fixture = setup();
    const options = fixture.debugElement.queryAll(By.css('option'));
    expect(options.length).toBe(4);
    expect(options[0].nativeElement.value).toBe('someone-off-list');
  });

  it('persists the selection and reloads on change', () => {
    (environment as { production: boolean }).production = false;
    const fixture = setup();
    const component = fixture.componentInstance;
    const reloadSpy = spyOn(component as unknown as { reload: () => void }, 'reload');
    const select: HTMLSelectElement = fixture.debugElement.query(By.css('select')).nativeElement;

    select.value = 'admin1';
    select.dispatchEvent(new Event('change'));

    expect(localStorage.getItem('devUser')).toBe('admin1');
    expect(reloadSpy).toHaveBeenCalled();
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MeApiService } from '../core/me-api.service';
import { SettingsComponent } from './settings.component';
import { LocaleService } from '../core/locale.service';

describe('SettingsComponent', () => {
  let fixture: ComponentFixture<SettingsComponent>;
  let meApi: jasmine.SpyObj<MeApiService>;
  let localeService: jasmine.SpyObj<LocaleService>;

  function setup(): void {
    meApi = jasmine.createSpyObj('MeApiService', ['get']);
    meApi.get.and.returnValue(Promise.resolve({
      id: 'u1', samAccountName: 's1', email: 's1@x.com', fullNameAr: 'أحمد', fullNameEn: 'Ahmed',
      department: 'IT', title: null, points: 0, level: 1, roles: ['submitter'],
    }));

    localeService = jasmine.createSpyObj('LocaleService', ['alternateLocaleHref']);
    localeService.alternateLocaleHref.and.returnValue('/ar/settings');

    TestBed.configureTestingModule({
      imports: [SettingsComponent],
      providers: [
        { provide: MeApiService, useValue: meApi },
        { provide: LocaleService, useValue: localeService },
      ],
    });
    fixture = TestBed.createComponent(SettingsComponent);
  }

  it('loads and displays the profile', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Ahmed');
    expect(fixture.nativeElement.textContent).toContain('s1@x.com');
    expect(fixture.nativeElement.textContent).toContain('submitter');
  });

  it('does not render a session/sign-out section', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent.toLowerCase()).not.toContain('sign out');
  });

  it('shows an error state with retry when the profile fetch fails, and recovers on retry', async () => {
    meApi = jasmine.createSpyObj('MeApiService', ['get']);
    meApi.get.and.returnValue(Promise.reject(new Error('boom')));
    localeService = jasmine.createSpyObj('LocaleService', ['alternateLocaleHref']);
    localeService.alternateLocaleHref.and.returnValue('/ar/settings');

    TestBed.configureTestingModule({
      imports: [SettingsComponent],
      providers: [
        { provide: MeApiService, useValue: meApi },
        { provide: LocaleService, useValue: localeService },
      ],
    });
    fixture = TestBed.createComponent(SettingsComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    meApi.get.and.returnValue(Promise.resolve({
      id: 'u1', samAccountName: 's1', email: 's1@x.com', fullNameAr: 'أحمد', fullNameEn: 'Ahmed',
      department: 'IT', title: null, points: 0, level: 1, roles: ['submitter'],
    }));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Ahmed');
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LOCALE_ID } from '@angular/core';
import { provideRouter } from '@angular/router';
import { PlatformSettingsApiService } from '../platform-settings-api.service';
import { SettingRow } from '../platform-settings.model';
import { PlatformSettingsComponent } from './platform-settings.component';

describe('PlatformSettingsComponent', () => {
  let fixture: ComponentFixture<PlatformSettingsComponent>;
  let api: jasmine.SpyObj<PlatformSettingsApiService>;

  const rows: SettingRow[] = [
    { key: 'top_n', valueJson: '5', updatedAt: null },
    { key: 'feature_flag', valueJson: 'true', updatedAt: null },
    { key: 'notification_rules', valueJson: '{"channels":["email"]}', updatedAt: null },
  ];

  function setup(settingRows: SettingRow[]): void {
    api = jasmine.createSpyObj('PlatformSettingsApiService', ['list', 'patch']);
    api.list.and.returnValue(Promise.resolve(settingRows));
    api.patch.and.callFake((key: string, valueJson: string) =>
      Promise.resolve({ key, valueJson, updatedAt: '2026-07-22T00:00:00Z' }),
    );

    TestBed.configureTestingModule({
      imports: [PlatformSettingsComponent],
      providers: [
        provideRouter([]),
        { provide: PlatformSettingsApiService, useValue: api },
        { provide: LOCALE_ID, useValue: 'en' },
      ],
    });
    fixture = TestBed.createComponent(PlatformSettingsComponent);
  }

  async function init(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('renders settings grouped by category', async () => {
    setup(rows);
    await init();

    const el = fixture.nativeElement as HTMLElement;
    const evaluationSection = el.querySelector('[data-group="evaluation"]');
    const generalSection = el.querySelector('[data-group="general"]');

    expect(evaluationSection).toBeTruthy();
    expect(generalSection).toBeTruthy();
    expect(evaluationSection?.querySelector('[data-key="top_n"]')).toBeTruthy();
    expect(generalSection?.querySelector('[data-key="feature_flag"]')).toBeTruthy();
    expect(generalSection?.querySelector('[data-key="notification_rules"]')).toBeTruthy();
    // top_n must not also appear under general, and feature_flag must not appear under evaluation.
    expect(evaluationSection?.querySelector('[data-key="feature_flag"]')).toBeFalsy();
    expect(generalSection?.querySelector('[data-key="top_n"]')).toBeFalsy();
  });

  it('infers a number input for a numeric setting and a checkbox toggle for a boolean setting', async () => {
    setup(rows);
    await init();

    const el = fixture.nativeElement as HTMLElement;
    const topNRow = el.querySelector('[data-key="top_n"]') as HTMLElement;
    const featureFlagRow = el.querySelector('[data-key="feature_flag"]') as HTMLElement;
    const jsonRow = el.querySelector('[data-key="notification_rules"]') as HTMLElement;

    expect(topNRow.querySelector('input[type="number"]')).toBeTruthy();
    expect(featureFlagRow.querySelector('input[type="checkbox"]')).toBeTruthy();
    expect(jsonRow.querySelector('textarea')).toBeTruthy();
  });

  it('saves an edited numeric setting as a stringified value', async () => {
    setup(rows);
    await init();

    fixture.componentInstance.updateValue('top_n', 7);
    await fixture.componentInstance.save('top_n');
    fixture.detectChanges();

    expect(api.patch).toHaveBeenCalledWith('top_n', '7');
    const updated = fixture.componentInstance.workingSettings().find((s) => s.key === 'top_n');
    expect(updated?.saveState).toBe('saved');
  });

  it('blocks the save and shows an inline error when the JSON textarea holds invalid JSON', async () => {
    setup(rows);
    await init();

    fixture.componentInstance.updateJsonText('notification_rules', '{invalid');
    await fixture.componentInstance.save('notification_rules');
    fixture.detectChanges();

    expect(api.patch).not.toHaveBeenCalledWith('notification_rules', jasmine.any(String));
    const updated = fixture.componentInstance.workingSettings().find((s) => s.key === 'notification_rules');
    expect(updated?.saveState).toBe('error');
    expect(updated?.error).toBeTruthy();

    const el = fixture.nativeElement as HTMLElement;
    const jsonRow = el.querySelector('[data-key="notification_rules"]') as HTMLElement;
    expect(jsonRow.querySelector('.field-error')?.textContent?.trim()).toBeTruthy();
  });

  it('shows an empty-state message when there are no settings', async () => {
    setup([]);
    await init();

    expect(fixture.nativeElement.textContent).toContain('No platform settings found');
  });

  it('sets a load error when the list call fails', async () => {
    api = jasmine.createSpyObj('PlatformSettingsApiService', ['list', 'patch']);
    api.list.and.returnValue(Promise.reject(new Error('boom')));

    TestBed.configureTestingModule({
      imports: [PlatformSettingsComponent],
      providers: [
        provideRouter([]),
        { provide: PlatformSettingsApiService, useValue: api },
        { provide: LOCALE_ID, useValue: 'en' },
      ],
    });
    fixture = TestBed.createComponent(PlatformSettingsComponent);
    await init();

    expect(fixture.componentInstance.loadError()).toBeTrue();
  });

  it('shows an error state with retry when the list call fails, and recovers on retry', async () => {
    api = jasmine.createSpyObj('PlatformSettingsApiService', ['list', 'patch']);
    api.list.and.returnValue(Promise.reject(new Error('boom')));

    TestBed.configureTestingModule({
      imports: [PlatformSettingsComponent],
      providers: [
        provideRouter([]),
        { provide: PlatformSettingsApiService, useValue: api },
        { provide: LOCALE_ID, useValue: 'en' },
      ],
    });
    fixture = TestBed.createComponent(PlatformSettingsComponent);
    await init();

    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    expect(retryButton).toBeTruthy();

    api.list.and.returnValue(Promise.resolve(rows));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeFalse();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('[data-group="evaluation"]')).toBeTruthy();
  });
});

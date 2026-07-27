// Change 20260726
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  NotificationCategory,
  NotificationPreference,
  NotificationsApiService,
} from '../../core/notifications-api.service';
import { NotificationPreferencesComponent } from './notification-preferences.component';

const categories: NotificationCategory[] = [
  { key: 'phase_announced', labelAr: 'إعلان مرحلة', labelEn: 'Phase Announced' },
  { key: 'idea_submitted', labelAr: 'تقديم فكرة', labelEn: 'Idea Submitted' },
];

function prefs(muted: Record<string, boolean>): NotificationPreference[] {
  return categories.map((c) => ({
    categoryKey: c.key,
    labelAr: c.labelAr,
    labelEn: c.labelEn,
    muted: muted[c.key] ?? false,
  }));
}

describe('NotificationPreferencesComponent', () => {
  let api: jasmine.SpyObj<NotificationsApiService>;
  let fixture: ComponentFixture<NotificationPreferencesComponent>;

  async function setup(locale = 'en-US'): Promise<NotificationPreferencesComponent> {
    api = jasmine.createSpyObj<NotificationsApiService>('NotificationsApiService', [
      'categories',
      'preferences',
      'updatePreferences',
    ]);
    api.categories.and.resolveTo(categories);
    api.preferences.and.resolveTo(prefs({ idea_submitted: true }));
    api.updatePreferences.and.resolveTo(prefs({ idea_submitted: true }));

    TestBed.configureTestingModule({
      imports: [NotificationPreferencesComponent],
      providers: [
        provideRouter([]),
        { provide: NotificationsApiService, useValue: api },
        { provide: LOCALE_ID, useValue: locale },
      ],
    });
    fixture = TestBed.createComponent(NotificationPreferencesComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('fetches categories and preferences on init and merges the muted flags', async () => {
    const c = await setup();

    expect(api.categories).toHaveBeenCalledTimes(1);
    expect(api.preferences).toHaveBeenCalledTimes(1);
    expect(c.rows().length).toBe(2);
    expect(c.rows()[0]).toEqual({ categoryKey: 'phase_announced', label: 'Phase Announced', muted: false });
    expect(c.rows()[1].muted).toBeTrue();
    expect(c.mutedCount()).toBe(1);
  });

  it('treats a category with no stored preference as unmuted', async () => {
    api = jasmine.createSpyObj<NotificationsApiService>('NotificationsApiService', [
      'categories',
      'preferences',
      'updatePreferences',
    ]);
    api.categories.and.resolveTo(categories);
    api.preferences.and.resolveTo([]);
    TestBed.configureTestingModule({
      imports: [NotificationPreferencesComponent],
      providers: [provideRouter([]), { provide: NotificationsApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(NotificationPreferencesComponent);
    await fixture.componentInstance.ngOnInit();

    expect(fixture.componentInstance.rows().every((r) => !r.muted)).toBeTrue();
  });

  it('uses the Arabic label under an Arabic locale', async () => {
    const c = await setup('ar-SA');

    expect(c.rows()[0].label).toBe('إعلان مرحلة');
  });

  it('renders a checkbox per category, checked when the category is not muted', async () => {
    await setup();

    const boxes = fixture.nativeElement.querySelectorAll('input[type="checkbox"]') as NodeListOf<HTMLInputElement>;
    expect(boxes.length).toBe(2);
    expect(boxes[0].checked).toBeTrue();
    expect(boxes[1].checked).toBeFalse();
  });

  it('PUTs the whole set when one row is toggled, inverting enabled into muted', async () => {
    const c = await setup();
    api.updatePreferences.and.resolveTo(prefs({ phase_announced: true, idea_submitted: true }));

    await c.toggle('phase_announced', false);

    expect(api.updatePreferences).toHaveBeenCalledWith([
      { categoryKey: 'phase_announced', muted: true },
      { categoryKey: 'idea_submitted', muted: true },
    ]);
    expect(c.rows()[0].muted).toBeTrue();
    expect(c.saved()).toBeTrue();
    expect(c.saveError()).toBeNull();
  });

  it('un-mutes a category when the box is checked back on', async () => {
    const c = await setup();
    api.updatePreferences.and.resolveTo(prefs({}));

    await c.toggle('idea_submitted', true);

    expect(api.updatePreferences).toHaveBeenCalledWith([
      { categoryKey: 'phase_announced', muted: false },
      { categoryKey: 'idea_submitted', muted: false },
    ]);
    expect(c.rows()[1].muted).toBeFalse();
  });

  it('rolls the row back and surfaces an error when the save fails', async () => {
    const c = await setup();
    api.updatePreferences.and.rejectWith(new Error('boom'));

    await c.toggle('phase_announced', false);

    expect(c.rows()[0].muted).toBeFalse();
    expect(c.saveError()).toContain('save');
    expect(c.saved()).toBeFalse();
  });

  it('shows an error state with retry when the initial load fails', async () => {
    api = jasmine.createSpyObj<NotificationsApiService>('NotificationsApiService', [
      'categories',
      'preferences',
      'updatePreferences',
    ]);
    api.categories.and.rejectWith(new Error('boom'));
    api.preferences.and.resolveTo([]);
    TestBed.configureTestingModule({
      imports: [NotificationPreferencesComponent],
      providers: [provideRouter([]), { provide: NotificationsApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(NotificationPreferencesComponent);
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-error-state')).not.toBeNull();
  });
});

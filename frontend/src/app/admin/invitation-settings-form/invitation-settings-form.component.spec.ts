import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { InvitationSettingsFormComponent } from './invitation-settings-form.component';

describe('InvitationSettingsFormComponent', () => {
  let fixture: ComponentFixture<InvitationSettingsFormComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvitationSettingsFormComponent, HttpClientTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(InvitationSettingsFormComponent);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    httpMock.expectOne('/api/admin/invitations/settings').flush({
      enabled: true,
      cronExpression: '0 9 * * 1',
      timezone: 'Asia/Riyadh',
      stopAfterNReminders: 3,
      gapHours: 48,
      expiresDays: 14,
      fromName: 'Innovation-to-Impact Program',
      fromEmail: 'noreply@gac.gov.sa',
      programNameAr: 'برنامج ابتكر لمنافس',
      programNameEn: 'Innovation-to-Impact Program',
      updatedAt: '2026-07-19T00:00:00Z',
    });
    httpMock.expectOne('/api/admin/roster/settings').flush({
      enabled: true,
      defaultExpiresDays: 14,
      reminderGapHours: 48,
      maxReminders: 3,
      updatedAt: '2026-07-19T00:00:00Z',
    });
    // ngOnInit awaits Promise.all([...]) — resolving both flushed requests takes an extra
    // microtask hop before the continuation (the two patchValue calls) runs. A macrotask
    // wait guarantees the microtask queue has fully drained before assertions run.
    await new Promise((resolve) => setTimeout(resolve));
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('shows an error state with retry when settings fail to load, and recovers on retry', async () => {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [InvitationSettingsFormComponent, HttpClientTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(InvitationSettingsFormComponent);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    httpMock.expectOne('/api/admin/invitations/settings').flush('Settings unavailable', { status: 500, statusText: 'Server Error' });
    httpMock.expectOne('/api/admin/roster/settings').flush({
      enabled: true,
      defaultExpiresDays: 14,
      reminderGapHours: 48,
      maxReminders: 3,
      updatedAt: '2026-07-19T00:00:00Z',
    });
    await new Promise((resolve) => setTimeout(resolve));
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).not.toBeNull();
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    retryButton.click();
    httpMock.expectOne('/api/admin/invitations/settings').flush({
      enabled: true,
      cronExpression: '0 9 * * 1',
      timezone: 'Asia/Riyadh',
      stopAfterNReminders: 3,
      gapHours: 48,
      expiresDays: 14,
      fromName: 'Innovation-to-Impact Program',
      fromEmail: 'noreply@gac.gov.sa',
      programNameAr: 'برنامج ابتكر لمنافس',
      programNameEn: 'Innovation-to-Impact Program',
      updatedAt: '2026-07-19T00:00:00Z',
    });
    httpMock.expectOne('/api/admin/roster/settings').flush({
      enabled: true,
      defaultExpiresDays: 14,
      reminderGapHours: 48,
      maxReminders: 3,
      updatedAt: '2026-07-19T00:00:00Z',
    });
    await new Promise((resolve) => setTimeout(resolve));
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.componentInstance.form.value.gapHours).toBe(48);
  });

  it('loads existing settings into the form', () => {
    expect(fixture.componentInstance.form.value.gapHours).toBe(48);
    expect(fixture.componentInstance.form.value.fromEmail).toBe('noreply@gac.gov.sa');
  });

  it('submits a PATCH with the current form values and shows a saved message', async () => {
    fixture.componentInstance.form.patchValue({ gapHours: 72 });
    fixture.componentInstance.onSubmit();

    const req = httpMock.expectOne('/api/admin/invitations/settings');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body.gapHours).toBe(72);
    req.flush({ ...fixture.componentInstance.form.getRawValue(), updatedAt: '2026-07-19T01:00:00Z' });

    await fixture.whenStable();
    expect(fixture.componentInstance.saved()).toBe(true);
  });

  it('loads existing role invitation settings into the second form', () => {
    expect(fixture.componentInstance.roleInvitationForm.value.reminderGapHours).toBe(48);
    expect(fixture.componentInstance.roleInvitationForm.value.maxReminders).toBe(3);
  });

  it('submits a PATCH with the role invitation form values and shows a saved message, independently of the first form', async () => {
    fixture.componentInstance.roleInvitationForm.patchValue({ maxReminders: 5 });
    fixture.componentInstance.onSubmitRoleInvitationSettings();

    const req = httpMock.expectOne('/api/admin/roster/settings');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body.maxReminders).toBe(5);
    req.flush({ ...fixture.componentInstance.roleInvitationForm.getRawValue(), updatedAt: '2026-07-19T01:00:00Z' });

    await fixture.whenStable();
    expect(fixture.componentInstance.roleInvitationSaved()).toBe(true);
    expect(fixture.componentInstance.saved()).toBe(false);
  });
});

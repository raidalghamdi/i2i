import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EmailTemplateApiService } from '../email-template-api.service';
import { EmailTemplate, EmailTemplateAttachment } from '../email-template.model';
import { EmailTemplateEditorComponent } from './email-template-editor.component';

describe('EmailTemplateEditorComponent', () => {
  let fixture: ComponentFixture<EmailTemplateEditorComponent>;
  let api: jasmine.SpyObj<EmailTemplateApiService>;

  const templates: EmailTemplate[] = [
    { id: 't-invite', kind: 'invite', subjectAr: 'أ', subjectEn: 'Invite', bodyAr: 'ب', bodyEn: 'Body', isBroadcast: false },
    { id: 't-accept', kind: 'accept', subjectAr: 'ق', subjectEn: 'Accept', bodyAr: 'ب2', bodyEn: 'Body2', isBroadcast: false },
    { id: 't-reject', kind: 'reject', subjectAr: 'ر', subjectEn: 'Reject', bodyAr: 'ب3', bodyEn: 'Body3', isBroadcast: false },
    { id: 't-reminder', kind: 'reminder', subjectAr: 'ت', subjectEn: 'Reminder', bodyAr: 'ب4', bodyEn: 'Body4', isBroadcast: false },
  ];

  function setup(attachments: EmailTemplateAttachment[] = []): void {
    api = jasmine.createSpyObj('EmailTemplateApiService', ['list', 'update', 'listAttachments', 'uploadAttachment', 'deleteAttachment']);
    api.list.and.returnValue(Promise.resolve(templates));
    api.listAttachments.and.returnValue(Promise.resolve(attachments));

    TestBed.configureTestingModule({
      imports: [EmailTemplateEditorComponent],
      providers: [{ provide: EmailTemplateApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(EmailTemplateEditorComponent);
  }

  it('loads templates and selects the first kind by default', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.activeKind()).toBe('invite');
    expect(fixture.nativeElement.textContent).toContain('Invite');
  });

  it('switches active kind and populates the form from the selected template', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    fixture.componentInstance.switchTo('reject');

    expect(fixture.componentInstance.activeKind()).toBe('reject');
    expect(fixture.componentInstance.subjectEn()).toBe('Reject');
  });

  it('saves the active template and refreshes it from the server response', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    const updated: EmailTemplate = { ...templates[0], subjectEn: 'Updated subject' };
    api.update.and.returnValue(Promise.resolve(updated));

    await fixture.componentInstance.save();

    expect(api.update).toHaveBeenCalledWith('t-invite', { subjectAr: 'أ', subjectEn: 'Invite', bodyAr: 'ب', bodyEn: 'Body', isBroadcast: false });
    expect(fixture.componentInstance.subjectEn()).toBe('Updated subject');
  });

  it('uploads an attachment and refreshes the attachment list', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    const uploaded: EmailTemplateAttachment = { id: 'att-1', fileName: 'a.pdf', contentType: 'application/pdf', fileSizeBytes: 100, uploadedAt: '2026-07-20T00:00:00Z' };
    api.uploadAttachment.and.returnValue(Promise.resolve(uploaded));
    api.listAttachments.and.returnValue(Promise.resolve([uploaded]));

    const file = new File(['x'], 'a.pdf', { type: 'application/pdf' });
    await fixture.componentInstance.onFileSelected(file);

    expect(api.uploadAttachment).toHaveBeenCalledWith('t-invite', file);
    expect(fixture.componentInstance.attachments().length).toBe(1);
  });

  it('deletes an attachment and refreshes the attachment list', async () => {
    const existing: EmailTemplateAttachment = { id: 'att-1', fileName: 'a.pdf', contentType: 'application/pdf', fileSizeBytes: 100, uploadedAt: '2026-07-20T00:00:00Z' };
    setup([existing]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    api.deleteAttachment.and.returnValue(Promise.resolve());
    api.listAttachments.and.returnValue(Promise.resolve([]));

    await fixture.componentInstance.onDeleteAttachment('att-1');

    expect(api.deleteAttachment).toHaveBeenCalledWith('att-1');
    expect(fixture.componentInstance.attachments().length).toBe(0);
  });

  it('shows an empty-state message when there are no templates', async () => {
    api = jasmine.createSpyObj('EmailTemplateApiService', ['list', 'update', 'listAttachments', 'uploadAttachment', 'deleteAttachment']);
    api.list.and.returnValue(Promise.resolve([]));

    TestBed.configureTestingModule({
      imports: [EmailTemplateEditorComponent],
      providers: [{ provide: EmailTemplateApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(EmailTemplateEditorComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No email templates configured yet.');
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    api = jasmine.createSpyObj('EmailTemplateApiService', ['list', 'update', 'listAttachments', 'uploadAttachment', 'deleteAttachment']);
    api.list.and.returnValue(Promise.reject({ error: { error: 'Templates unavailable' } }));

    TestBed.configureTestingModule({
      imports: [EmailTemplateEditorComponent],
      providers: [{ provide: EmailTemplateApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(EmailTemplateEditorComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Templates unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    api.list.and.returnValue(Promise.resolve(templates));
    api.listAttachments.and.returnValue(Promise.resolve([]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Invite');
  });
});

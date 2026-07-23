import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LOCALE_ID } from '@angular/core';
import { provideRouter } from '@angular/router';
import { EmailLogApiService } from '../email-log-api.service';
import { EmailLogListResult, EmailLogRow } from '../email-log.model';
import { EmailLogComponent } from './email-log.component';

describe('EmailLogComponent', () => {
  let fixture: ComponentFixture<EmailLogComponent>;
  let emailLogApi: jasmine.SpyObj<EmailLogApiService>;

  const sentRow: EmailLogRow = {
    id: 'e1',
    provider: 'smtp',
    statusCode: 'sent',
    statusNameAr: 'أُرسل',
    statusNameEn: 'Sent',
    providerMessageId: 'msg-123',
    redirectApplied: false,
    toEmail: 'alice@example.com',
    sentAt: '2026-01-05T10:00:00Z',
  };

  const redirectedRow: EmailLogRow = {
    id: 'e2',
    provider: 'smtp',
    statusCode: 'failed',
    statusNameAr: 'فشل',
    statusNameEn: 'Failed',
    providerMessageId: null,
    redirectApplied: true,
    toEmail: 'bob@example.com',
    sentAt: '2026-01-06T11:00:00Z',
  };

  function setup(result: EmailLogListResult): void {
    emailLogApi = jasmine.createSpyObj('EmailLogApiService', ['list']);
    emailLogApi.list.and.returnValue(Promise.resolve(result));

    TestBed.configureTestingModule({
      imports: [EmailLogComponent],
      providers: [
        provideRouter([]),
        { provide: EmailLogApiService, useValue: emailLogApi },
        { provide: LOCALE_ID, useValue: 'en' },
      ],
    });
    fixture = TestBed.createComponent(EmailLogComponent);
  }

  async function init(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('renders one row per email log entry', async () => {
    setup({ items: [sentRow, redirectedRow], total: 2, page: 1, pageSize: 25 });
    await init();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('alice@example.com');
    expect(text).toContain('bob@example.com');
    expect(text).toContain('msg-123');
  });

  it('shows an empty-state message when there are no log entries', async () => {
    setup({ items: [], total: 0, page: 1, pageSize: 25 });
    await init();

    expect(fixture.nativeElement.textContent).toContain('No email log entries');
  });

  it('shows a redirected chip only for rows with redirectApplied', async () => {
    setup({ items: [sentRow, redirectedRow], total: 2, page: 1, pageSize: 25 });
    await init();

    const chips = fixture.nativeElement.querySelectorAll('.email-log-redirected');
    expect(chips.length).toBe(1);
  });

  it('re-queries with the selected status filter and resets to page 1', async () => {
    setup({ items: [sentRow], total: 1, page: 1, pageSize: 25 });
    await init();

    emailLogApi.list.calls.reset();
    fixture.componentInstance.page.set(3);
    fixture.componentInstance.statusFilter.set('failed');
    await fixture.componentInstance.onFilterChange();

    expect(emailLogApi.list).toHaveBeenCalledWith({ status: 'failed', page: 1, pageSize: 25 });
    expect(fixture.componentInstance.page()).toBe(1);
  });

  it('advances to the next page and reloads', async () => {
    setup({ items: [sentRow], total: 50, page: 1, pageSize: 25 });
    await init();

    emailLogApi.list.calls.reset();
    await fixture.componentInstance.onNext();

    expect(fixture.componentInstance.page()).toBe(2);
    expect(emailLogApi.list).toHaveBeenCalledWith(jasmine.objectContaining({ page: 2 }));
  });

  it('does not advance past the last page', async () => {
    setup({ items: [sentRow], total: 1, page: 1, pageSize: 25 });
    await init();

    emailLogApi.list.calls.reset();
    await fixture.componentInstance.onNext();

    expect(emailLogApi.list).not.toHaveBeenCalled();
    expect(fixture.componentInstance.page()).toBe(1);
  });

  it('sets an error message when the list call fails', async () => {
    emailLogApi = jasmine.createSpyObj('EmailLogApiService', ['list']);
    emailLogApi.list.and.returnValue(Promise.reject(new Error('boom')));

    TestBed.configureTestingModule({
      imports: [EmailLogComponent],
      providers: [
        provideRouter([]),
        { provide: EmailLogApiService, useValue: emailLogApi },
        { provide: LOCALE_ID, useValue: 'en' },
      ],
    });
    fixture = TestBed.createComponent(EmailLogComponent);
    await init();

    expect(fixture.componentInstance.errorMessage()).toBeTruthy();
  });

  it('shows an error state with retry when the list call fails, and recovers on retry', async () => {
    emailLogApi = jasmine.createSpyObj('EmailLogApiService', ['list']);
    emailLogApi.list.and.returnValue(Promise.reject(new Error('boom')));

    TestBed.configureTestingModule({
      imports: [EmailLogComponent],
      providers: [
        provideRouter([]),
        { provide: EmailLogApiService, useValue: emailLogApi },
        { provide: LOCALE_ID, useValue: 'en' },
      ],
    });
    fixture = TestBed.createComponent(EmailLogComponent);
    await init();

    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    expect(retryButton).toBeTruthy();

    emailLogApi.list.and.returnValue(Promise.resolve({ items: [sentRow], total: 1, page: 1, pageSize: 25 }));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('alice@example.com');
  });
});

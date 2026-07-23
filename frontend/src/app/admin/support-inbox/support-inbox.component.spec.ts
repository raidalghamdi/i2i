import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SupportApiService } from '../support-api.service';
import { SupportListResult, SupportRow } from '../support.model';
import { SupportInboxComponent } from './support-inbox.component';

describe('SupportInboxComponent', () => {
  let fixture: ComponentFixture<SupportInboxComponent>;
  let supportApi: jasmine.SpyObj<SupportApiService>;

  const newRow: SupportRow = {
    id: 's1',
    name: 'Jane Doe',
    email: 'jane@example.com',
    subject: 'Cannot submit idea',
    body: 'I get an error when submitting my idea.',
    handled: false,
    createdAt: '2026-01-05T10:00:00Z',
  };

  const handledRow: SupportRow = {
    id: 's2',
    name: 'John Roe',
    email: 'john@example.com',
    subject: 'Question about tracks',
    body: 'Which track should I pick?',
    handled: true,
    createdAt: '2026-01-06T11:00:00Z',
  };

  function setup(result: SupportListResult): void {
    supportApi = jasmine.createSpyObj('SupportApiService', ['list', 'markHandled']);
    supportApi.list.and.returnValue(Promise.resolve(result));
    supportApi.markHandled.and.returnValue(Promise.resolve());

    TestBed.configureTestingModule({
      imports: [SupportInboxComponent],
      providers: [provideRouter([]), { provide: SupportApiService, useValue: supportApi }],
    });
    fixture = TestBed.createComponent(SupportInboxComponent);
  }

  async function init(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('renders one row per support message', async () => {
    setup({ items: [newRow, handledRow], total: 2, page: 1, pageSize: 25 });
    await init();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Jane Doe');
    expect(text).toContain('jane@example.com');
    expect(text).toContain('Cannot submit idea');
    expect(text).toContain('John Roe');
  });

  it('shows an empty-state message when there are no support messages', async () => {
    setup({ items: [], total: 0, page: 1, pageSize: 25 });
    await init();

    expect(fixture.nativeElement.textContent).toContain('No support messages');
  });

  it('shows a Mark handled button only for unhandled rows', async () => {
    setup({ items: [newRow, handledRow], total: 2, page: 1, pageSize: 25 });
    await init();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')).filter(
      (b) => (b as HTMLButtonElement).textContent?.trim() === 'Mark handled',
    );
    expect(buttons.length).toBe(1);
  });

  it('marks a message handled and reloads the list', async () => {
    setup({ items: [newRow], total: 1, page: 1, pageSize: 25 });
    await init();

    supportApi.list.and.returnValue(Promise.resolve({ items: [handledRow], total: 1, page: 1, pageSize: 25 }));

    await fixture.componentInstance.onMarkHandled('s1');

    expect(supportApi.markHandled).toHaveBeenCalledWith('s1');
    expect(supportApi.list).toHaveBeenCalledTimes(2);
    expect(fixture.componentInstance.rows()[0].handled).toBeTrue();
  });

  it('sets an error message when marking handled fails', async () => {
    setup({ items: [newRow], total: 1, page: 1, pageSize: 25 });
    await init();

    supportApi.markHandled.and.returnValue(Promise.reject(new Error('boom')));

    await fixture.componentInstance.onMarkHandled('s1');

    expect(fixture.componentInstance.errorMessage()).toContain('Could not mark as handled');
  });

  it('re-queries with the selected handled filter and resets to page 1', async () => {
    setup({ items: [newRow], total: 1, page: 1, pageSize: 25 });
    await init();

    supportApi.list.calls.reset();
    fixture.componentInstance.page.set(3);
    fixture.componentInstance.handledFilter.set('true');
    await fixture.componentInstance.onFilterChange();

    expect(supportApi.list).toHaveBeenCalledWith({ handled: true, page: 1, pageSize: 25 });
    expect(fixture.componentInstance.page()).toBe(1);
  });

  it('omits the handled param when the filter is All', async () => {
    setup({ items: [newRow], total: 1, page: 1, pageSize: 25 });
    await init();

    supportApi.list.calls.reset();
    fixture.componentInstance.handledFilter.set('');
    await fixture.componentInstance.onFilterChange();

    expect(supportApi.list).toHaveBeenCalledWith({ handled: undefined, page: 1, pageSize: 25 });
  });

  it('advances to the next page and reloads', async () => {
    setup({ items: [newRow], total: 50, page: 1, pageSize: 25 });
    await init();

    supportApi.list.calls.reset();
    await fixture.componentInstance.onNext();

    expect(fixture.componentInstance.page()).toBe(2);
    expect(supportApi.list).toHaveBeenCalledWith(jasmine.objectContaining({ page: 2 }));
  });

  it('shows an error state with retry when the list call fails, and recovers on retry', async () => {
    supportApi = jasmine.createSpyObj('SupportApiService', ['list', 'markHandled']);
    supportApi.list.and.returnValue(Promise.reject(new Error('boom')));
    TestBed.configureTestingModule({
      imports: [SupportInboxComponent],
      providers: [provideRouter([]), { provide: SupportApiService, useValue: supportApi }],
    });
    fixture = TestBed.createComponent(SupportInboxComponent);
    await init();

    expect(fixture.componentInstance.loadError()).toBeTruthy();
    const retryButton: HTMLButtonElement = fixture.nativeElement.querySelector('app-error-state button');
    expect(retryButton).toBeTruthy();

    supportApi.list.and.returnValue(Promise.resolve({ items: [newRow], total: 1, page: 1, pageSize: 25 }));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Jane Doe');
  });
});

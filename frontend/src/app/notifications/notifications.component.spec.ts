import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotificationStore } from '../core/notification-store';
import { NotificationsApiService, NotificationItem } from '../core/notifications-api.service';
import { NotificationsComponent } from './notifications.component';

describe('NotificationsComponent', () => {
  let fixture: ComponentFixture<NotificationsComponent>;
  let api: jasmine.SpyObj<NotificationsApiService>;
  let store: NotificationStore;

  const items: NotificationItem[] = [
    { id: 'n1', notificationType: 'idea_status', titleAr: 'ت1', titleEn: 'T1', bodyAr: 'ب1', bodyEn: 'B1', link: null, readAt: null, createdAt: '2026-07-02T00:00:00Z' },
    { id: 'n2', notificationType: 'idea_status', titleAr: 'ت2', titleEn: 'T2', bodyAr: 'ب2', bodyEn: 'B2', link: null, readAt: '2026-07-01T00:00:00Z', createdAt: '2026-07-01T00:00:00Z' },
  ];

  function setup(): void {
    api = jasmine.createSpyObj('NotificationsApiService', ['list', 'markRead', 'markAllRead']);
    api.list.and.returnValue(Promise.resolve(items));

    TestBed.configureTestingModule({
      imports: [NotificationsComponent],
      providers: [{ provide: NotificationsApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(NotificationsComponent);
    store = TestBed.inject(NotificationStore);
  }

  it('loads all notifications from the store by default', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.filtered().length).toBe(2);
    expect(fixture.componentInstance.store.notifications().length).toBe(2);
  });

  it('filters to unread only', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.componentInstance.filter.set('unread');
    fixture.detectChanges();

    expect(fixture.componentInstance.filtered().length).toBe(1);
    expect(fixture.componentInstance.filtered()[0].id).toBe('n1');
  });

  it('marks a single notification read via the store and updates shared state', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    api.markRead.and.returnValue(Promise.resolve({ ...items[0], readAt: '2026-07-03T00:00:00Z' }));
    spyOn(store, 'markRead').and.callThrough();
    await fixture.componentInstance.dismiss('n1');

    expect(store.markRead).toHaveBeenCalledWith('n1');
    expect(api.markRead).toHaveBeenCalledWith('n1');
    expect(store.notifications().find((n) => n.id === 'n1')?.readAt).toBe('2026-07-03T00:00:00Z');
    // the shared store reflects the change, so the shell badge (unreadCount) drops too
    expect(store.unreadCount()).toBe(0);
  });

  it('marks all read via the store and clears unread count', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    api.markAllRead.and.returnValue(Promise.resolve({ markedCount: 1 }));
    spyOn(store, 'markAllRead').and.callThrough();
    await fixture.componentInstance.markAllRead();

    expect(store.markAllRead).toHaveBeenCalled();
    expect(api.markAllRead).toHaveBeenCalled();
    expect(store.notifications().every((n) => n.readAt !== null)).toBe(true);
    expect(store.unreadCount()).toBe(0);
  });

  it('shows an empty state when there are no notifications', async () => {
    api = jasmine.createSpyObj('NotificationsApiService', ['list', 'markRead', 'markAllRead']);
    api.list.and.returnValue(Promise.resolve([]));
    TestBed.configureTestingModule({
      imports: [NotificationsComponent],
      providers: [{ provide: NotificationsApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(NotificationsComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No notifications.');
  });

  it('shows an error state with retry when the list load fails, and recovers on retry', async () => {
    api = jasmine.createSpyObj('NotificationsApiService', ['list', 'markRead', 'markAllRead']);
    api.list.and.returnValue(Promise.reject(new Error('boom')));
    TestBed.configureTestingModule({
      imports: [NotificationsComponent],
      providers: [{ provide: NotificationsApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(NotificationsComponent);
    store = TestBed.inject(NotificationStore);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    api.list.and.returnValue(Promise.resolve(items));
    retryButton.click();
    await store.refresh();
    fixture.detectChanges();

    expect(store.notifications().length).toBe(2);
  });
});

import { TestBed } from '@angular/core/testing';
import { NOTIFICATION_POLL_INTERVAL_MS, NotificationStore } from './notification-store';
import { NotificationItem, NotificationsApiService } from './notifications-api.service';

const POLL_INTERVAL_MS = 20;

function makeItem(id: string, readAt: string | null): NotificationItem {
  return {
    id,
    notificationType: 'idea_status',
    titleAr: 'ت',
    titleEn: 'T',
    bodyAr: 'ب',
    bodyEn: 'B',
    link: null,
    readAt,
    createdAt: '2026-07-01T00:00:00Z',
  };
}

describe('NotificationStore', () => {
  let store: NotificationStore;
  let api: jasmine.SpyObj<NotificationsApiService>;

  beforeEach(() => {
    api = jasmine.createSpyObj<NotificationsApiService>('NotificationsApiService', ['list', 'markRead', 'markAllRead']);
    api.list.and.resolveTo([makeItem('n1', null), makeItem('n2', '2026-07-01T00:00:00Z')]);

    TestBed.configureTestingModule({
      providers: [
        { provide: NOTIFICATION_POLL_INTERVAL_MS, useValue: POLL_INTERVAL_MS },
        { provide: NotificationsApiService, useValue: api },
      ],
    });

    store = TestBed.inject(NotificationStore);
  });

  afterEach(() => {
    store.stop();
  });

  it('start() immediately refreshes and populates notifications/unreadCount', async () => {
    store.start();
    // refresh() is fired synchronously but resolves asynchronously; wait a tick for the promise to settle.
    await Promise.resolve();
    await Promise.resolve();

    expect(api.list).toHaveBeenCalledTimes(1);
    expect(store.notifications().length).toBe(2);
    expect(store.unreadCount()).toBe(1);
  });

  it('polls again after the interval elapses, and stops polling after stop()', async () => {
    store.start();
    await Promise.resolve();
    await Promise.resolve();
    expect(api.list).toHaveBeenCalledTimes(1);

    await new Promise((resolve) => setTimeout(resolve, POLL_INTERVAL_MS * 2));
    expect(api.list.calls.count()).toBeGreaterThanOrEqual(2);

    store.stop();
    const countAfterStop = api.list.calls.count();
    await new Promise((resolve) => setTimeout(resolve, POLL_INTERVAL_MS * 2));
    expect(api.list.calls.count()).toBe(countAfterStop);
  });

  it('markRead() updates the api and drops unreadCount immediately', async () => {
    store.start();
    await Promise.resolve();
    await Promise.resolve();
    expect(store.unreadCount()).toBe(1);

    const updated = makeItem('n1', '2026-07-03T00:00:00Z');
    api.markRead.and.resolveTo(updated);

    await store.markRead('n1');

    expect(api.markRead).toHaveBeenCalledWith('n1');
    expect(store.unreadCount()).toBe(0);
    expect(store.notifications().find((n: NotificationItem) => n.id === 'n1')?.readAt).toBe('2026-07-03T00:00:00Z');
  });

  it('markAllRead() updates the api and clears unreadCount locally', async () => {
    store.start();
    await Promise.resolve();
    await Promise.resolve();
    expect(store.unreadCount()).toBe(1);

    api.markAllRead.and.resolveTo({ markedCount: 1 });

    await store.markAllRead();

    expect(api.markAllRead).toHaveBeenCalledTimes(1);
    expect(store.unreadCount()).toBe(0);
  });

  it('does not poll while document.hidden is true, but refreshes when visibility returns', async () => {
    let hidden = true;
    spyOnProperty(document, 'hidden', 'get').and.callFake(() => hidden);

    store.start();
    await Promise.resolve();
    await Promise.resolve();
    // The initial refresh() call is explicit (not gated by document.hidden), so it always runs.
    expect(api.list).toHaveBeenCalledTimes(1);

    await new Promise((resolve) => setTimeout(resolve, POLL_INTERVAL_MS * 2));
    // Interval ticks are gated by document.hidden, so no further calls should have happened.
    expect(api.list).toHaveBeenCalledTimes(1);

    hidden = false;
    document.dispatchEvent(new Event('visibilitychange'));
    await Promise.resolve();
    await Promise.resolve();
    expect(api.list.calls.count()).toBeGreaterThanOrEqual(2);
  });
});

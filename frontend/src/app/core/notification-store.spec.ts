import { TestBed } from '@angular/core/testing';
import { NOTIFICATION_POLL_INTERVAL_MS, NotificationStore } from './notification-store';
import { NotificationItem, NotificationsApiService } from './notifications-api.service';
// Change 20260726
import { NotificationHubService, NotificationPush } from './notification-hub.service';

const POLL_INTERVAL_MS = 20;

// Change 20260726 — stands in for the SignalR connection so specs can push events synchronously.
class FakeNotificationHubService {
  onNotification: ((push: NotificationPush) => void) | null = null;
  onReconnected: (() => void) | null = null;
  disconnectCalls = 0;
  startError: Error | null = null;

  async connect(
    onNotification: (push: NotificationPush) => void,
    onReconnected?: () => void,
  ): Promise<void> {
    if (this.startError) throw this.startError;
    this.onNotification = onNotification;
    this.onReconnected = onReconnected ?? null;
  }

  async disconnect(): Promise<void> {
    this.disconnectCalls++;
    this.onNotification = null;
  }
}

function makePush(id: string): NotificationPush {
  return {
    id,
    notificationType: 'idea_status',
    titleAr: 'ت',
    titleEn: 'T',
    bodyAr: 'ب',
    bodyEn: 'B',
    link: null,
  };
}

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
  let hub: FakeNotificationHubService; // Change 20260726

  beforeEach(() => {
    api = jasmine.createSpyObj<NotificationsApiService>('NotificationsApiService', ['list', 'markRead', 'markAllRead']);
    api.list.and.resolveTo([makeItem('n1', null), makeItem('n2', '2026-07-01T00:00:00Z')]);
    hub = new FakeNotificationHubService(); // Change 20260726

    TestBed.configureTestingModule({
      providers: [
        { provide: NOTIFICATION_POLL_INTERVAL_MS, useValue: POLL_INTERVAL_MS },
        { provide: NotificationsApiService, useValue: api },
        { provide: NotificationHubService, useValue: hub }, // Change 20260726
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

  // Change 20260726
  describe('SignalR push handling', () => {
    async function startAndSettle(): Promise<void> {
      store.start();
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
    }

    it('connects to the hub on start() and marks the store live', async () => {
      await startAndSettle();

      expect(hub.onNotification).not.toBeNull();
      expect(store.live()).toBeTrue();
    });

    it('prepends a pushed notification and increments the unread count', async () => {
      await startAndSettle();
      expect(store.notifications().length).toBe(2);
      expect(store.unreadCount()).toBe(1);

      hub.onNotification!(makePush('n3'));

      expect(store.notifications().length).toBe(3);
      expect(store.notifications()[0].id).toBe('n3');
      expect(store.notifications()[0].readAt).toBeNull();
      expect(store.unreadCount()).toBe(2);
    });

    it('ignores a pushed notification the poll already delivered', async () => {
      await startAndSettle();

      hub.onNotification!(makePush('n1'));

      expect(store.notifications().length).toBe(2);
      expect(store.unreadCount()).toBe(1);
    });

    it('resyncs from the API after a reconnect, since pushes may have been missed', async () => {
      await startAndSettle();
      const countAfterStart = api.list.calls.count();

      hub.onReconnected!();
      await Promise.resolve();
      await Promise.resolve();

      expect(api.list.calls.count()).toBe(countAfterStart + 1);
    });

    it('keeps polling when the handshake fails instead of surfacing an error', async () => {
      hub.startError = new Error('handshake refused');

      await startAndSettle();

      expect(store.live()).toBeFalse();
      expect(store.error()).toBeNull();
      expect(store.notifications().length).toBe(2);
    });

    it('disconnects the hub on stop()', async () => {
      await startAndSettle();

      store.stop();

      expect(hub.disconnectCalls).toBe(1);
      expect(store.live()).toBeFalse();
    });
  });
});

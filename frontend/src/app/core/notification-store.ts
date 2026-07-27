import { Injectable, InjectionToken, computed, inject, signal } from '@angular/core';
import { NotificationItem, NotificationsApiService } from './notifications-api.service';
// Change 20260726
import { NotificationHubService, NotificationPush } from './notification-hub.service';

// Change 20260726 — halved in frequency: SignalR now delivers in real time, so polling is only a
// safety net for a failed handshake or a push missed mid-reconnect.
export const NOTIFICATION_POLL_INTERVAL_MS = new InjectionToken<number>('NOTIFICATION_POLL_INTERVAL_MS', {
  providedIn: 'root',
  factory: () => 60_000,
});

@Injectable({ providedIn: 'root' })
export class NotificationStore {
  private readonly api = inject(NotificationsApiService);
  private readonly intervalMs = inject(NOTIFICATION_POLL_INTERVAL_MS);
  private readonly hub = inject(NotificationHubService); // Change 20260726

  readonly notifications = signal<NotificationItem[]>([]);
  readonly unreadCount = computed(() => this.notifications().filter((n) => n.readAt === null).length);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  /** Change 20260726 — false while the handshake is pending or after it failed (poll-only mode). */
  readonly live = signal(false);

  private timer: ReturnType<typeof setInterval> | null = null;
  private readonly onVisibility = (): void => {
    if (!document.hidden) void this.refresh();
  };

  start(): void {
    if (this.timer !== null) return; // idempotent
    void this.refresh();
    this.timer = setInterval(() => {
      if (!document.hidden) void this.refresh();
    }, this.intervalMs);
    document.addEventListener('visibilitychange', this.onVisibility);
    void this.connectHub(); // Change 20260726
  }

  stop(): void {
    if (this.timer !== null) {
      clearInterval(this.timer);
      this.timer = null;
    }
    document.removeEventListener('visibilitychange', this.onVisibility);
    // Change 20260726
    this.live.set(false);
    void this.hub.disconnect();
  }

  // Change 20260726
  private async connectHub(): Promise<void> {
    try {
      await this.hub.connect(
        (push) => this.applyPush(push),
        // A drop may have swallowed pushes, so resync from the API rather than trusting the delta.
        () => void this.refresh(),
      );
      this.live.set(true);
    } catch {
      // Handshake failed (no token, hub unreachable). Polling already covers us; stay quiet.
      this.live.set(false);
    }
  }

  // Change 20260726
  private applyPush(push: NotificationPush): void {
    this.notifications.update((items) => {
      if (items.some((n) => n.id === push.id)) return items; // a poll may have raced us to it
      const item: NotificationItem = {
        ...push,
        readAt: null,
        createdAt: new Date().toISOString(),
      };
      return [item, ...items];
    });
  }

  async refresh(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.notifications.set(await this.api.list());
    } catch {
      this.error.set($localize`:@@notificationsLoadError:Couldn't load notifications. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }

  async markRead(id: string): Promise<void> {
    const updated = await this.api.markRead(id);
    this.notifications.update((items) => items.map((n) => (n.id === id ? updated : n)));
  }

  async markAllRead(): Promise<void> {
    await this.api.markAllRead();
    this.notifications.update((items) =>
      items.map((n) => (n.readAt === null ? { ...n, readAt: new Date().toISOString() } : n)),
    );
  }
}

import { Injectable, InjectionToken, computed, inject, signal } from '@angular/core';
import { NotificationItem, NotificationsApiService } from './notifications-api.service';

export const NOTIFICATION_POLL_INTERVAL_MS = new InjectionToken<number>('NOTIFICATION_POLL_INTERVAL_MS', {
  providedIn: 'root',
  factory: () => 30_000,
});

@Injectable({ providedIn: 'root' })
export class NotificationStore {
  private readonly api = inject(NotificationsApiService);
  private readonly intervalMs = inject(NOTIFICATION_POLL_INTERVAL_MS);

  readonly notifications = signal<NotificationItem[]>([]);
  readonly unreadCount = computed(() => this.notifications().filter((n) => n.readAt === null).length);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

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
  }

  stop(): void {
    if (this.timer !== null) {
      clearInterval(this.timer);
      this.timer = null;
    }
    document.removeEventListener('visibilitychange', this.onVisibility);
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

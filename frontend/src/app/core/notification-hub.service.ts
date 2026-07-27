// Change 20260726
import { Injectable, inject } from '@angular/core';
// Type-only: the runtime import is dynamic (see build()) to keep ~59 kB of SignalR out of the
// initial bundle, since this service is reachable from the always-present notification bell.
import type { HubConnection, IRetryPolicy, RetryContext } from '@microsoft/signalr';
import { TokenStorageService } from './auth/token-storage.service';

/**
 * Shape pushed by SignalRNotificationPublisher. Deliberately narrower than NotificationItem:
 * the server omits readAt/createdAt on the push, so the store fills those in.
 */
export interface NotificationPush {
  id: string;
  notificationType: string;
  titleAr: string;
  titleEn: string;
  bodyAr: string;
  bodyEn: string;
  link: string | null;
}

export const NOTIFICATION_HUB_PATH = '/hubs/notifications';
export const RECEIVE_NOTIFICATION_EVENT = 'ReceiveNotification';

/**
 * Doubles the delay each attempt up to a 30s ceiling, then keeps retrying forever; SignalR's
 * default policy gives up after ~60s, which would silently downgrade a long outage to poll-only.
 * Half the delay is randomised so a server restart doesn't bring every client back at once.
 */
export class ExponentialBackoffRetryPolicy implements IRetryPolicy {
  constructor(
    private readonly maxDelayMs = 30_000,
    private readonly random: () => number = Math.random,
  ) {}

  nextRetryDelayInMilliseconds(context: RetryContext): number {
    const ceiling = Math.min(1_000 * 2 ** context.previousRetryCount, this.maxDelayMs);
    return Math.round(ceiling / 2 + this.random() * (ceiling / 2));
  }
}

@Injectable({ providedIn: 'root' })
export class NotificationHubService {
  private readonly tokens = inject(TokenStorageService);
  private connection: HubConnection | null = null;

  get connected(): boolean {
    return this.connection !== null;
  }

  /**
   * Resolves once connected. Rejects if the initial handshake fails so the caller can fall back to
   * polling; automatic reconnect only covers drops that happen after a successful start.
   */
  async connect(
    onNotification: (push: NotificationPush) => void,
    onReconnected?: () => void,
  ): Promise<void> {
    if (this.connection !== null) return;

    const connection = await this.build();
    this.connection = connection;
    connection.on(RECEIVE_NOTIFICATION_EVENT, onNotification);
    if (onReconnected) connection.onreconnected(() => onReconnected());

    try {
      await connection.start();
    } catch (error) {
      this.connection = null;
      throw error;
    }
  }

  async disconnect(): Promise<void> {
    const connection = this.connection;
    if (connection === null) return;
    this.connection = null;
    connection.off(RECEIVE_NOTIFICATION_EVENT);
    await connection.stop();
  }

  protected async build(): Promise<HubConnection> {
    const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');
    return new HubConnectionBuilder()
      .withUrl(NOTIFICATION_HUB_PATH, {
        accessTokenFactory: () => this.tokens.getAccessToken() ?? '',
      })
      .withAutomaticReconnect(new ExponentialBackoffRetryPolicy())
      .configureLogging(LogLevel.Warning)
      .build();
  }
}

import { Component, LOCALE_ID, OnInit, computed, inject, signal } from '@angular/core';
import { NotificationStore } from '../core/notification-store';
import { NotificationItem } from '../core/notifications-api.service';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../shared/error-state/error-state.component';

@Component({
  selector: 'app-notifications',
  imports: [PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './notifications.component.html',
})
export class NotificationsComponent implements OnInit {
  readonly store = inject(NotificationStore);
  private readonly isArabic = inject(LOCALE_ID).toString().startsWith('ar'); // Change 20260726

  readonly filter = signal<'all' | 'unread'>('all');

  // Change 20260726
  title(n: NotificationItem): string {
    return this.isArabic ? n.titleAr : n.titleEn;
  }

  // Change 20260726
  body(n: NotificationItem): string {
    return this.isArabic ? n.bodyAr : n.bodyEn;
  }

  readonly filtered = computed(() =>
    this.filter() === 'unread'
      ? this.store.notifications().filter((n) => n.readAt === null)
      : this.store.notifications()
  );

  async ngOnInit(): Promise<void> {
    await this.store.refresh();
  }

  async dismiss(id: string): Promise<void> {
    await this.store.markRead(id);
  }

  async markAllRead(): Promise<void> {
    await this.store.markAllRead();
  }
}

import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { NotificationStore } from '../core/notification-store';
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

  readonly filter = signal<'all' | 'unread'>('all');

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

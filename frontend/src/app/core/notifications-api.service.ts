import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface NotificationItem {
  id: string;
  notificationType: string;
  titleAr: string;
  titleEn: string;
  bodyAr: string;
  bodyEn: string;
  link: string | null;
  readAt: string | null;
  createdAt: string;
}

// Change 20260726
export interface NotificationCategory {
  key: string;
  labelAr: string;
  labelEn: string;
}

// Change 20260726
export interface NotificationPreference {
  categoryKey: string;
  labelAr: string;
  labelEn: string;
  muted: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<NotificationItem[]> {
    return firstValueFrom(this.http.get<NotificationItem[]>('/api/notifications'));
  }

  markRead(id: string): Promise<NotificationItem> {
    return firstValueFrom(this.http.post<NotificationItem>(`/api/notifications/${id}/read`, null));
  }

  markAllRead(): Promise<{ markedCount: number }> {
    return firstValueFrom(this.http.post<{ markedCount: number }>('/api/notifications/read-all', null));
  }

  // Change 20260726
  categories(): Promise<NotificationCategory[]> {
    return firstValueFrom(this.http.get<NotificationCategory[]>('/api/notifications/categories'));
  }

  // Change 20260726
  preferences(): Promise<NotificationPreference[]> {
    return firstValueFrom(this.http.get<NotificationPreference[]>('/api/notifications/preferences'));
  }

  /**
   * Change 20260726 — sends every category rather than just the toggled one, and returns the
   * server's re-read of the full set so the UI can't drift from what was actually persisted.
   */
  updatePreferences(preferences: { categoryKey: string; muted: boolean }[]): Promise<NotificationPreference[]> {
    return firstValueFrom(
      this.http.put<NotificationPreference[]>('/api/notifications/preferences', { preferences }),
    );
  }
}

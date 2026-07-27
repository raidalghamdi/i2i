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
}

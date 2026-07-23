import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NotificationBellComponent } from './notification-bell.component';
import { NotificationStore } from '../../core/notification-store';
import { NotificationItem, NotificationsApiService } from '../../core/notifications-api.service';

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

describe('NotificationBellComponent', () => {
  let fixture: ComponentFixture<NotificationBellComponent>;
  let api: jasmine.SpyObj<NotificationsApiService>;

  function setup(): void {
    api = jasmine.createSpyObj<NotificationsApiService>('NotificationsApiService', ['list', 'markRead', 'markAllRead']);
    api.list.and.resolveTo([]);
    TestBed.configureTestingModule({
      imports: [NotificationBellComponent],
      providers: [provideRouter([]), { provide: NotificationsApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(NotificationBellComponent);
  }

  it('links to /notifications', () => {
    setup();
    fixture.detectChanges();
    const link = fixture.nativeElement.querySelector('a');
    expect(link.getAttribute('href')).toBe('/notifications');
  });

  it('hides the badge when unreadCount is 0', () => {
    setup();
    fixture.detectChanges();
    const badge = fixture.nativeElement.querySelector('span');
    expect(badge).toBeNull();
  });

  it('shows the exact count when unreadCount is between 1 and 9', async () => {
    setup();
    api.list.and.resolveTo([makeItem('n1', null), makeItem('n2', null), makeItem('n3', '2026-07-01T00:00:00Z')]);
    const store = TestBed.inject(NotificationStore);
    await store.refresh();
    fixture.detectChanges();
    const badge = fixture.nativeElement.querySelector('span');
    expect(badge.textContent.trim()).toBe('2');
  });

  it('shows "9+" when unreadCount exceeds 9', async () => {
    setup();
    const items = Array.from({ length: 12 }, (_, i) => makeItem(`n${i}`, null));
    api.list.and.resolveTo(items);
    const store = TestBed.inject(NotificationStore);
    await store.refresh();
    fixture.detectChanges();
    const badge = fixture.nativeElement.querySelector('span');
    expect(badge.textContent.trim()).toBe('9+');
  });
});

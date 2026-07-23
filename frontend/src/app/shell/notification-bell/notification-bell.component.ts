import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NotificationStore } from '../../core/notification-store';
import { IconComponent } from '../../shared/icon/icon.component';

/** Header bell button linking to the notifications page, showing a live
 * unread-count badge sourced from the shared NotificationStore. */
@Component({
  selector: 'app-notification-bell',
  imports: [RouterLink, IconComponent],
  templateUrl: './notification-bell.component.html',
})
export class NotificationBellComponent {
  readonly store = inject(NotificationStore);

  protected readonly ariaLabel = $localize`:@@notificationBellAria:Notifications`;
}

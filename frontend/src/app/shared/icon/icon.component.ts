import { Component, input } from '@angular/core';

export type IconName =
  | 'dashboard'
  | 'lightbulb'
  | 'clipboard-check'
  | 'users'
  | 'target'
  | 'settings'
  | 'chart-bar'
  | 'document-text'
  | 'alert-triangle'
  | 'shield-check'
  | 'stack'
  | 'globe'
  | 'plus'
  | 'menu'
  | 'check-circle'
  | 'clipboard-list'
  | 'sparkles'
  | 'rocket'
  | 'wrench'
  | 'expand'
  | 'presentation'
  | 'award'
  | 'image'
  | 'play-circle'
  | 'chevron-down'
  | 'arrow-right'
  | 'arrow-up'
  | 'building'
  | 'linkedin'
  | 'twitter'
  | 'youtube'
  | 'panel-left'
  | 'x'
  | 'upload'
  | 'download'
  | 'calendar'
  | 'search'
  | 'gavel'
  | 'shield-alert'
  | 'heart-pulse'
  | 'message-square'
  | 'bell';

/** Hand-drawn 24x24 outline icon set (stroke-based, Heroicons-weight) — no icon-library dependency. */
@Component({
  selector: 'app-icon',
  templateUrl: './icon.component.html',
  host: { '[class]': "'inline-flex shrink-0 ' + size()" },
})
export class IconComponent {
  readonly name = input.required<IconName>();
  readonly size = input<string>('h-4 w-4');
}

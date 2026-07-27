// Change 20260726
import { Component, LOCALE_ID, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import {
  NotificationPreference,
  NotificationsApiService,
} from '../../core/notifications-api.service';

interface PreferenceRow {
  categoryKey: string;
  label: string;
  muted: boolean;
}

@Component({
  selector: 'app-notification-preferences',
  imports: [PageHeaderComponent, LoadingStateComponent, ErrorStateComponent, RouterLink],
  templateUrl: './notification-preferences.component.html',
})
export class NotificationPreferencesComponent implements OnInit {
  private readonly api = inject(NotificationsApiService);
  private readonly isArabic = inject(LOCALE_ID).toString().startsWith('ar');

  readonly rows = signal<PreferenceRow[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly saved = signal(false);

  readonly mutedCount = computed(() => this.rows().filter((r) => r.muted).length);

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      // Categories are the authoritative list and order; preferences only carry the muted flag,
      // and a category the user has never touched has no preference row at all.
      const [categories, preferences] = await Promise.all([
        this.api.categories(),
        this.api.preferences(),
      ]);
      const mutedByKey = new Map(preferences.map((p) => [p.categoryKey, p.muted]));
      this.rows.set(
        categories.map((c) => ({
          categoryKey: c.key,
          label: this.isArabic ? c.labelAr : c.labelEn,
          muted: mutedByKey.get(c.key) ?? false,
        })),
      );
    } catch {
      this.error.set(
        $localize`:@@notificationPreferencesLoadError:Couldn't load notification preferences. Please try again.`,
      );
    } finally {
      this.loading.set(false);
    }
  }

  /** Optimistic: flip locally, then PUT the whole set; roll back the row if the save fails. */
  async toggle(categoryKey: string, enabled: boolean): Promise<void> {
    const muted = !enabled;
    const previous = this.rows();
    this.rows.update((rows) => rows.map((r) => (r.categoryKey === categoryKey ? { ...r, muted } : r)));

    this.saving.set(true);
    this.saveError.set(null);
    this.saved.set(false);
    try {
      const updated = await this.api.updatePreferences(
        this.rows().map((r) => ({ categoryKey: r.categoryKey, muted: r.muted })),
      );
      this.applyServerState(updated);
      this.saved.set(true);
    } catch {
      this.rows.set(previous);
      this.saveError.set(
        $localize`:@@notificationPreferencesSaveError:Couldn't save your preferences. Please try again.`,
      );
    } finally {
      this.saving.set(false);
    }
  }

  private applyServerState(updated: NotificationPreference[]): void {
    const mutedByKey = new Map(updated.map((p) => [p.categoryKey, p.muted]));
    this.rows.update((rows) =>
      rows.map((r) => ({ ...r, muted: mutedByKey.get(r.categoryKey) ?? r.muted })),
    );
  }
}

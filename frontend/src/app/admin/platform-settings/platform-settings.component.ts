import { Component, Inject, LOCALE_ID, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { PlatformSettingsApiService } from '../platform-settings-api.service';
import { SettingRow } from '../platform-settings.model';

export type SettingType = 'boolean' | 'number' | 'string' | 'json';
export type SaveState = 'idle' | 'saving' | 'saved' | 'error';

export interface WorkingSetting {
  key: string;
  value: unknown;
  type: SettingType;
  /** Only populated (and edited) for type 'json' — the raw textarea contents. */
  jsonText?: string;
  saveState: SaveState;
  error?: string;
  updatedAt: string | null;
}

interface SettingMeta {
  labelAr: string;
  labelEn: string;
  group: string;
}

interface SettingGroup {
  group: string;
  settings: WorkingSetting[];
}

const SETTING_META: Record<string, SettingMeta> = {
  top_n: { labelEn: 'Top N (final ranking)', labelAr: 'أفضل N', group: 'evaluation' },
  pass_threshold: { labelEn: 'Pass threshold', labelAr: 'درجة النجاح', group: 'evaluation' },
};

const GROUP_ORDER = ['evaluation', 'general'];

@Component({
  selector: 'app-platform-settings',
  imports: [FormsModule, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './platform-settings.component.html',
})
export class PlatformSettingsComponent implements OnInit {
  private readonly api = inject(PlatformSettingsApiService);
  private readonly isArabic: boolean;

  readonly settings = signal<SettingRow[]>([]);
  readonly workingSettings = signal<WorkingSetting[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  /** Display message for the load-error state (kept separate from the boolean `loadError` flag, which existing tests assert on directly). */
  readonly loadErrorMessage = signal<string | null>(null);
  protected readonly defaultLoadErrorMessage = $localize`:@@settingsLoadError:Could not load platform settings. Please try again.`;

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);
    this.loadErrorMessage.set(null);
    try {
      const rows = await this.api.list();
      this.settings.set(rows);
      this.workingSettings.set(rows.map((row) => this.toWorking(row)));
    } catch (error) {
      this.loadError.set(true);
      this.loadErrorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  groupedSettings(): SettingGroup[] {
    const byGroup = new Map<string, WorkingSetting[]>();
    for (const setting of this.workingSettings()) {
      const group = this.groupFor(setting.key);
      const bucket = byGroup.get(group);
      if (bucket) {
        bucket.push(setting);
      } else {
        byGroup.set(group, [setting]);
      }
    }
    const orderedGroups = [...GROUP_ORDER, ...[...byGroup.keys()].filter((g) => !GROUP_ORDER.includes(g))];
    return orderedGroups.filter((g) => byGroup.has(g)).map((g) => ({ group: g, settings: byGroup.get(g)! }));
  }

  groupFor(key: string): string {
    return SETTING_META[key]?.group ?? 'general';
  }

  label(key: string): string {
    const meta = SETTING_META[key];
    if (!meta) return key;
    return this.isArabic ? meta.labelAr : meta.labelEn;
  }

  groupLabel(group: string): string {
    if (group === 'evaluation') {
      return $localize`:@@settingsGroupEvaluation:Evaluation`;
    }
    return $localize`:@@settingsGroupGeneral:General`;
  }

  asBoolean(setting: WorkingSetting): boolean {
    return setting.value as boolean;
  }

  asNumber(setting: WorkingSetting): number {
    return setting.value as number;
  }

  asString(setting: WorkingSetting): string {
    return setting.value as string;
  }

  updateValue(key: string, value: unknown): void {
    this.workingSettings.update((list) =>
      list.map((s) => (s.key === key ? { ...s, value, saveState: 'idle', error: undefined } : s)),
    );
  }

  updateJsonText(key: string, jsonText: string): void {
    this.workingSettings.update((list) =>
      list.map((s) => (s.key === key ? { ...s, jsonText, saveState: 'idle', error: undefined } : s)),
    );
  }

  async save(key: string): Promise<void> {
    const setting = this.workingSettings().find((s) => s.key === key);
    if (!setting) return;

    let jsonString: string;
    if (setting.type === 'json') {
      try {
        const parsed = JSON.parse(setting.jsonText ?? '');
        jsonString = JSON.stringify(parsed);
      } catch {
        this.setState(key, 'error', $localize`:@@settingsInvalidJson:Invalid JSON.`);
        return;
      }
    } else {
      jsonString = JSON.stringify(setting.value);
    }

    this.setState(key, 'saving');
    try {
      const updated = await this.api.patch(key, jsonString);
      this.workingSettings.update((list) =>
        list.map((s) => (s.key === key ? { ...this.toWorking(updated), saveState: 'saved' } : s)),
      );
    } catch (error) {
      this.setState(key, 'error', this.extractErrorMessage(error));
    }
  }

  private setState(key: string, saveState: SaveState, error?: string): void {
    this.workingSettings.update((list) =>
      list.map((s) => (s.key === key ? { ...s, saveState, error } : s)),
    );
  }

  private toWorking(row: SettingRow): WorkingSetting {
    let type: SettingType;
    let value: unknown;
    let jsonText: string | undefined;
    try {
      const parsed = JSON.parse(row.valueJson);
      if (typeof parsed === 'boolean') {
        type = 'boolean';
        value = parsed;
      } else if (typeof parsed === 'number') {
        type = 'number';
        value = parsed;
      } else if (typeof parsed === 'string') {
        type = 'string';
        value = parsed;
      } else {
        type = 'json';
        value = parsed;
        jsonText = JSON.stringify(parsed, null, 2);
      }
    } catch {
      type = 'string';
      value = row.valueJson;
    }
    return { key: row.key, value, type, jsonText, saveState: 'idle', updatedAt: row.updatedAt };
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`Something went wrong. Please try again.`;
  }
}

import { Component, OnInit, inject, signal } from '@angular/core';
import { MeApiService, MeProfile } from '../core/me-api.service';
import { LocaleService } from '../core/locale.service';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../shared/error-state/error-state.component';

@Component({
  selector: 'app-settings',
  imports: [PageHeaderComponent, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './settings.component.html',
})
export class SettingsComponent implements OnInit {
  private readonly meApi = inject(MeApiService);
  private readonly localeService = inject(LocaleService);

  readonly profile = signal<MeProfile | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.profile.set(await this.meApi.get());
    } catch (err) {
      this.error.set(this.extractErrorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  alternateLocaleHref(): string {
    return this.localeService.alternateLocaleHref();
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@userSettingsLoadError:Couldn't load your settings. Please try again.`;
  }
}

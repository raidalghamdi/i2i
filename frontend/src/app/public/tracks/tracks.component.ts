import { Component, Inject, LOCALE_ID, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { PublicTracksApiService } from '../../core/public-tracks-api.service';
import { PublicTrack } from '../../core/public-data.model';

@Component({
  selector: 'app-tracks',
  imports: [
    PublicPageHeroComponent,
    IconComponent,
    RouterLink,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  templateUrl: './tracks.component.html',
})
export class TracksComponent implements OnInit {
  private readonly api = inject(PublicTracksApiService);
  private readonly isArabic: boolean;

  readonly pageTitle = $localize`:@@tracksTitle:Competition Tracks`;
  readonly pageBody = $localize`:@@tracksBody:Explore the themes shaping this year's competition and find where your idea fits best.`;

  readonly tracks = signal<PublicTrack[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.tracks.set(await this.api.list());
    } catch (err) {
      this.error.set(this.extractErrorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  name(theme: PublicTrack): string {
    return this.isArabic ? theme.nameAr : theme.nameEn;
  }

  description(theme: PublicTrack): string {
    return this.isArabic ? theme.descriptionAr : theme.descriptionEn;
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@tracksLoadError:Couldn't load tracks. Please try again.`;
  }
}

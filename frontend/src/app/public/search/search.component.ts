import { Component, Inject, LOCALE_ID, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { PublicSearchApiService } from '../../core/public-search-api.service';
import { PublicIdeaSummary, PublicTrack } from '../../core/public-data.model';

@Component({
  selector: 'app-search',
  imports: [
    PublicPageHeroComponent,
    IconComponent,
    RouterLink,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  templateUrl: './search.component.html',
})
export class SearchComponent implements OnInit {
  private readonly api = inject(PublicSearchApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly isArabic: boolean;

  readonly pageTitle = $localize`:@@searchTitle:Search`;
  readonly pageBody = $localize`:@@searchBody:Search ideas and tracks across the program.`;

  readonly query = signal('');
  readonly ideas = signal<PublicIdeaSummary[]>([]);
  readonly tracks = signal<PublicTrack[]>([]);
  readonly loading = signal(false);
  readonly ran = signal(false);
  readonly error = signal<string | null>(null);

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((params) => {
      const q = (params.get('q') ?? '').trim();
      this.query.set(q);
      if (q) {
        void this.run(q);
      } else {
        this.ideas.set([]);
        this.tracks.set([]);
        this.ran.set(false);
      }
    });
  }

  onSubmit(event: Event): void {
    event.preventDefault();
    const q = this.query().trim();
    void this.router.navigate(['/search'], { queryParams: q ? { q } : {} });
  }

  reload(): void {
    const q = this.query().trim();
    if (q) void this.run(q);
  }

  private async run(q: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const results = await this.api.search(q);
      this.ideas.set(results.ideas);
      this.tracks.set(results.tracks);
    } catch (err) {
      this.error.set(this.extractErrorMessage(err));
      this.ideas.set([]);
      this.tracks.set([]);
    } finally {
      this.loading.set(false);
      this.ran.set(true);
    }
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@searchLoadError:Couldn't search. Please try again.`;
  }

  get totalCount(): number {
    return this.ideas().length + this.tracks().length;
  }

  ideaTitle(idea: PublicIdeaSummary): string {
    return this.isArabic ? idea.titleAr : idea.titleEn;
  }

  trackName(track: PublicTrack): string {
    return this.isArabic ? track.nameAr : track.nameEn;
  }

  trackDescription(track: PublicTrack): string {
    return this.isArabic ? track.descriptionAr : track.descriptionEn;
  }
}

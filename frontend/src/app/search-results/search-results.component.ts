import { Component, Inject, LOCALE_ID, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SearchApiService } from '../core/search-api.service';
import { SearchResultItem, SearchResults } from '../core/search.model';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../shared/error-state/error-state.component';

/** Full-page authenticated search results, reached from the header search's
 * "View all results" and direct `/app-search?q=...` navigation. Reacts to
 * `q` query-param changes so re-searching from the header refetches here. */
@Component({
  selector: 'app-search-results',
  imports: [RouterLink, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './search-results.component.html',
})
export class SearchResultsComponent implements OnInit {
  private readonly searchApi = inject(SearchApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly isArabic: boolean;

  protected readonly pageTitle = $localize`:@@searchResultsTitle:Search results`;
  protected readonly ideasLabel = $localize`:@@headerSearchIdeas:Ideas`;
  protected readonly challengesLabel = $localize`:@@headerSearchChallenges:Challenges`;
  protected readonly usersLabel = $localize`:@@headerSearchUsers:Users`;
  protected readonly promptTitle = $localize`:@@searchResultsPrompt:Enter a search term`;
  protected readonly noResultsTitle = $localize`:@@searchResultsNoResults:No results`;

  readonly query = signal('');
  readonly results = signal<SearchResults | null>(null);
  readonly loading = signal(false);
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
        this.results.set(null);
        this.error.set(null);
        this.loading.set(false);
      }
    });
  }

  reload(): void {
    const q = this.query().trim();
    if (q) void this.run(q);
  }

  private async run(q: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const res = await this.searchApi.search(q);
      this.results.set(res);
    } catch (err) {
      this.error.set(this.extractErrorMessage(err));
      this.results.set(null);
    } finally {
      this.loading.set(false);
    }
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@searchResultsLoadError:Couldn't search. Please try again.`;
  }

  get totalCount(): number {
    const r = this.results();
    if (!r) return 0;
    return r.ideas.length + r.challenges.length + r.users.length;
  }

  itemTitle(item: SearchResultItem): string {
    return this.isArabic ? item.titleAr : item.titleEn;
  }
}

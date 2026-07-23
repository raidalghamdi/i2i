import { Component, ElementRef, HostListener, Inject, LOCALE_ID, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, Subscription, from } from 'rxjs';
import { debounceTime, switchMap } from 'rxjs/operators';
import { SearchApiService } from '../../core/search-api.service';
import { SearchResultItem, SearchResults } from '../../core/search.model';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { IconComponent } from '../../shared/icon/icon.component';

/** Debounced global search box for the authenticated app header: shows a
 * grouped (ideas/challenges/users) results dropdown and is keyboard-navigable. */
@Component({
  selector: 'app-header-search',
  imports: [LoadingStateComponent, EmptyStateComponent, IconComponent],
  templateUrl: './header-search.component.html',
})
export class HeaderSearchComponent {
  private readonly searchApi = inject(SearchApiService);
  private readonly router = inject(Router);
  private readonly elementRef = inject(ElementRef<HTMLElement>);
  private readonly isArabic: boolean;

  protected readonly placeholder = $localize`:@@headerSearchPlaceholder:Search…`;
  protected readonly noResultsTitle = $localize`:@@headerSearchNoResults:No results`;
  protected readonly ideasLabel = $localize`:@@headerSearchIdeas:Ideas`;
  protected readonly challengesLabel = $localize`:@@headerSearchChallenges:Challenges`;
  protected readonly usersLabel = $localize`:@@headerSearchUsers:Users`;
  protected readonly viewAllLabel = $localize`:@@headerSearchViewAll:View all results`;

  readonly query = signal('');
  readonly results = signal<SearchResults | null>(null);
  readonly loading = signal(false);
  readonly open = signal(false);
  readonly highlightedIndex = signal(-1);

  readonly flatResults = computed<SearchResultItem[]>(() => {
    const r = this.results();
    if (!r) return [];
    return [...r.ideas, ...r.challenges, ...r.users];
  });

  readonly totalCount = computed(() => this.flatResults().length);

  private readonly queryInput$ = new Subject<string>();
  private readonly searchSub: Subscription;

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
    this.searchSub = this.queryInput$
      .pipe(
        debounceTime(250),
        switchMap((q) => from(this.runSearch(q))),
      )
      .subscribe();
  }

  private async runSearch(q: string): Promise<void> {
    if (q.trim() === '') {
      this.results.set(null);
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    try {
      const res = await this.searchApi.search(q);
      this.results.set(res);
    } finally {
      this.loading.set(false);
      this.highlightedIndex.set(-1);
    }
  }

  /** Updates the query signal and (re)opens the dropdown; the actual search
   * request is debounced through `queryInput$`. Public so the template's
   * `(input)` handler and tests can both drive it directly. */
  setQuery(value: string): void {
    this.query.set(value);
    this.open.set(true);
    this.highlightedIndex.set(-1);
    this.queryInput$.next(value);
  }

  onInput(event: Event): void {
    this.setQuery((event.target as HTMLInputElement).value);
  }

  onFocus(): void {
    if (this.query().trim() !== '') {
      this.open.set(true);
    }
  }

  itemTitle(item: SearchResultItem): string {
    return this.isArabic ? item.titleAr : item.titleEn;
  }

  isHighlighted(item: SearchResultItem): boolean {
    const idx = this.highlightedIndex();
    return idx >= 0 && this.flatResults()[idx] === item;
  }

  selectItem(item: SearchResultItem): void {
    this.router.navigateByUrl(item.link);
    this.close(true);
  }

  viewAll(): void {
    const q = this.query().trim();
    this.router.navigateByUrl(`/app-search?q=${encodeURIComponent(q)}`);
    this.close(true);
  }

  onKeydown(event: KeyboardEvent): void {
    switch (event.key) {
      case 'ArrowDown': {
        event.preventDefault();
        const total = this.totalCount();
        if (total === 0) return;
        this.open.set(true);
        this.highlightedIndex.update((i) => (i + 1 >= total ? 0 : i + 1));
        return;
      }
      case 'ArrowUp': {
        event.preventDefault();
        const total = this.totalCount();
        if (total === 0) return;
        this.open.set(true);
        this.highlightedIndex.update((i) => (i - 1 < 0 ? total - 1 : i - 1));
        return;
      }
      case 'Enter': {
        event.preventDefault();
        const idx = this.highlightedIndex();
        const item = idx >= 0 ? this.flatResults()[idx] : undefined;
        if (item) {
          this.selectItem(item);
        } else if (this.query().trim() !== '') {
          this.viewAll();
        }
        return;
      }
      case 'Escape': {
        event.preventDefault();
        this.clear();
        return;
      }
    }
  }

  private clear(): void {
    this.query.set('');
    this.results.set(null);
    this.close(false);
  }

  private close(clearQuery: boolean): void {
    this.open.set(false);
    this.highlightedIndex.set(-1);
    if (clearQuery) {
      this.query.set('');
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
    }
  }
}

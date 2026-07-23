import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { SearchResultsComponent } from './search-results.component';
import { SearchApiService } from '../core/search-api.service';
import { SearchResults } from '../core/search.model';

describe('SearchResultsComponent', () => {
  let fixture: ComponentFixture<SearchResultsComponent>;
  let searchApiSpy: jasmine.SpyObj<SearchApiService>;
  let queryParamMap$: BehaviorSubject<ReturnType<typeof convertToParamMap>>;

  const sampleResults: SearchResults = {
    ideas: [
      { type: 'idea', id: 'i1', titleEn: 'Solar Idea', titleAr: 'فكرة شمسية', subtitle: 'Submitted', link: '/ideas/i1' },
    ],
    challenges: [
      { type: 'challenge', id: 'c1', titleEn: 'Water Challenge', titleAr: 'تحدي المياه', subtitle: 'Open', link: '/challenges/c1' },
    ],
    users: [
      { type: 'user', id: 'u1', titleEn: 'Jane Doe', titleAr: 'جين دو', subtitle: 'Evaluator', link: '/admin/users/u1' },
    ],
  };

  async function setup(initialQ: string | null): Promise<void> {
    searchApiSpy = jasmine.createSpyObj('SearchApiService', ['search']);
    queryParamMap$ = new BehaviorSubject(convertToParamMap(initialQ === null ? {} : { q: initialQ }));

    await TestBed.configureTestingModule({
      imports: [SearchResultsComponent],
      providers: [
        provideRouter([]),
        { provide: SearchApiService, useValue: searchApiSpy },
        { provide: ActivatedRoute, useValue: { queryParamMap: queryParamMap$ } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SearchResultsComponent);
  }

  it('reads q from the query params, calls search(q), and renders grouped results', async () => {
    await setup('idea');
    searchApiSpy.search.and.returnValue(Promise.resolve(sampleResults));

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(searchApiSpy.search).toHaveBeenCalledWith('idea');
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Solar Idea');
    expect(text).toContain('Water Challenge');
    expect(text).toContain('Jane Doe');
    expect(text).toContain('Ideas');
    expect(text).toContain('Challenges');
    expect(text).toContain('Users');
  });

  it('links results to item.link via routerLink', async () => {
    await setup('idea');
    searchApiSpy.search.and.returnValue(Promise.resolve(sampleResults));

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const hrefs = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('a')).map((a) =>
      a.getAttribute('href'),
    );
    expect(hrefs).toContain('/ideas/i1');
    expect(hrefs).toContain('/challenges/c1');
    expect(hrefs).toContain('/admin/users/u1');
  });

  it('does not call the API when q is empty and shows a prompt', async () => {
    await setup(null);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(searchApiSpy.search).not.toHaveBeenCalled();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Enter a search term');
  });

  it('shows a loading state while the search is in flight', async () => {
    await setup('idea');
    let resolveFn!: (value: SearchResults) => void;
    searchApiSpy.search.and.returnValue(new Promise((resolve) => (resolveFn = resolve)));

    fixture.detectChanges();
    expect(fixture.componentInstance.loading()).toBeTrue();

    resolveFn(sampleResults);
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.componentInstance.loading()).toBeFalse();
  });

  it('shows an empty state when the search resolves with zero total results', async () => {
    await setup('zzz');
    searchApiSpy.search.and.returnValue(Promise.resolve({ ideas: [], challenges: [], users: [] }));

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('No results');
  });

  it('shows an error state on rejection, and retry re-calls the API', async () => {
    await setup('idea');
    searchApiSpy.search.and.returnValue(Promise.reject(new Error('boom')));

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).not.toBeNull();
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    searchApiSpy.search.and.returnValue(Promise.resolve(sampleResults));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(searchApiSpy.search).toHaveBeenCalledTimes(2);
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Solar Idea');
  });

  it('refetches when navigating to a new q', async () => {
    await setup('idea');
    searchApiSpy.search.and.returnValue(Promise.resolve(sampleResults));

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(searchApiSpy.search).toHaveBeenCalledWith('idea');

    const secondResults: SearchResults = {
      ideas: [],
      challenges: [],
      users: [
        { type: 'user', id: 'u2', titleEn: 'John Roe', titleAr: 'جون رو', subtitle: 'Admin', link: '/admin/users/u2' },
      ],
    };
    searchApiSpy.search.and.returnValue(Promise.resolve(secondResults));
    queryParamMap$.next(convertToParamMap({ q: 'john' }));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(searchApiSpy.search).toHaveBeenCalledWith('john');
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('John Roe');
    expect(text).not.toContain('Solar Idea');
  });
});

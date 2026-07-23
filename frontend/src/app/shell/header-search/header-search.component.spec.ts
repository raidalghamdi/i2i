import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LOCALE_ID } from '@angular/core';
import { Router, provideRouter } from '@angular/router';
import { HeaderSearchComponent } from './header-search.component';
import { SearchApiService } from '../../core/search-api.service';
import { SearchResults } from '../../core/search.model';

/** This workspace is zoneless (no zone.js/testing), so `fakeAsync`/`tick`
 * aren't available here. The component's debounce is real RxJS `debounceTime`,
 * so tests wait out the real 250ms window instead of faking the clock. */
function wait(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

describe('HeaderSearchComponent', () => {
  let fixture: ComponentFixture<HeaderSearchComponent>;
  let component: HeaderSearchComponent;
  let searchApiSpy: jasmine.SpyObj<SearchApiService>;
  let router: Router;

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

  function setup() {
    searchApiSpy = jasmine.createSpyObj('SearchApiService', ['search']);
    TestBed.configureTestingModule({
      imports: [HeaderSearchComponent],
      providers: [
        provideRouter([]),
        { provide: SearchApiService, useValue: searchApiSpy },
        { provide: LOCALE_ID, useValue: 'en-US' },
      ],
    });
    fixture = TestBed.createComponent(HeaderSearchComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  }

  it('does not call the API before the debounce elapses, and calls it once after', async () => {
    setup();
    searchApiSpy.search.and.returnValue(Promise.resolve(sampleResults));

    component.setQuery('idea');
    expect(searchApiSpy.search).not.toHaveBeenCalled();

    await wait(300);
    expect(searchApiSpy.search).toHaveBeenCalledWith('idea');
    expect(searchApiSpy.search).toHaveBeenCalledTimes(1);
  });

  it('renders results grouped by type once the search resolves', async () => {
    setup();
    searchApiSpy.search.and.returnValue(Promise.resolve(sampleResults));

    component.setQuery('idea');
    await wait(300);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Solar Idea');
    expect(text).toContain('Water Challenge');
    expect(text).toContain('Jane Doe');
    expect(text).toContain('Ideas');
    expect(text).toContain('Challenges');
    expect(text).toContain('Users');
  });

  it('navigates to the item link when a result is clicked', async () => {
    setup();
    searchApiSpy.search.and.returnValue(Promise.resolve(sampleResults));
    spyOn(router, 'navigateByUrl');

    component.setQuery('idea');
    await wait(300);
    fixture.detectChanges();

    const button: HTMLButtonElement | null = fixture.nativeElement.querySelector('[data-result-id="i1"]');
    expect(button).not.toBeNull();
    button!.click();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/ideas/i1');
    expect(component.open()).toBeFalse();
  });

  it('shows the empty state when the search resolves with zero results', async () => {
    setup();
    searchApiSpy.search.and.returnValue(Promise.resolve({ ideas: [], challenges: [], users: [] }));

    component.setQuery('nomatch');
    await wait(300);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No results');
  });

  it('shows the loading state while the search is in flight', async () => {
    setup();
    let resolveFn!: (value: SearchResults) => void;
    searchApiSpy.search.and.returnValue(new Promise((resolve) => (resolveFn = resolve)));

    component.setQuery('idea');
    await wait(280);
    fixture.detectChanges();

    expect(component.loading()).toBeTrue();

    resolveFn(sampleResults);
    await wait(0);
    fixture.detectChanges();
    expect(component.loading()).toBeFalse();
  });

  it('moves the highlighted index with ArrowDown/ArrowUp and navigates on Enter', async () => {
    setup();
    searchApiSpy.search.and.returnValue(Promise.resolve(sampleResults));
    spyOn(router, 'navigateByUrl');

    component.setQuery('idea');
    await wait(300);
    fixture.detectChanges();

    component.onKeydown(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    expect(component.highlightedIndex()).toBe(0);
    component.onKeydown(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    expect(component.highlightedIndex()).toBe(1);
    component.onKeydown(new KeyboardEvent('keydown', { key: 'ArrowUp' }));
    expect(component.highlightedIndex()).toBe(0);

    component.onKeydown(new KeyboardEvent('keydown', { key: 'Enter' }));
    expect(router.navigateByUrl).toHaveBeenCalledWith('/ideas/i1');
  });

  it('navigates to the app-search page with the query when Enter is pressed with nothing highlighted', async () => {
    setup();
    searchApiSpy.search.and.returnValue(Promise.resolve(sampleResults));
    spyOn(router, 'navigateByUrl');

    component.setQuery('idea');
    await wait(300);
    fixture.detectChanges();

    component.onKeydown(new KeyboardEvent('keydown', { key: 'Enter' }));
    expect(router.navigateByUrl).toHaveBeenCalledWith('/app-search?q=idea');
  });

  it('clears and closes on Escape', async () => {
    setup();
    searchApiSpy.search.and.returnValue(Promise.resolve(sampleResults));

    component.setQuery('idea');
    await wait(300);
    fixture.detectChanges();
    expect(component.open()).toBeTrue();

    component.onKeydown(new KeyboardEvent('keydown', { key: 'Escape' }));
    expect(component.open()).toBeFalse();
    expect(component.query()).toBe('');
  });
});

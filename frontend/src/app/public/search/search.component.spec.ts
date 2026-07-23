import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { SearchComponent } from './search.component';
import { PublicSearchApiService } from '../../core/public-search-api.service';
import { PublicSearchResults } from '../../core/public-data.model';

class StubSearchApi {
  lastQuery = '';
  result: PublicSearchResults = {
    ideas: [{ id: 'i1', code: 'I-1', titleAr: 'فكرة', titleEn: 'Solar Idea', status: 'approved' }],
    tracks: [{ id: 't1', nameAr: 'مسار', nameEn: 'Green Track', descriptionAr: 'وصف', descriptionEn: 'desc', priority: 1 }],
  };
  search(q: string): Promise<PublicSearchResults> {
    this.lastQuery = q;
    return Promise.resolve(this.result);
  }
}

describe('SearchComponent', () => {
  let fixture: ComponentFixture<SearchComponent>;
  let api: StubSearchApi;

  async function setup(q: string | null): Promise<void> {
    api = new StubSearchApi();
    await TestBed.configureTestingModule({
      imports: [SearchComponent],
      providers: [
        provideRouter([]),
        { provide: PublicSearchApiService, useValue: api },
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: of(convertToParamMap(q === null ? {} : { q })) },
        },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(SearchComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('runs the search from the q query param and renders grouped results', async () => {
    await setup('solar');
    expect(api.lastQuery).toBe('solar');
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Solar Idea');
    expect(text).toContain('Green Track');
  });

  it('links idea hits to /ideas/:id and track hits to /tracks/:id', async () => {
    await setup('solar');
    const hrefs = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('a')).map((a) => a.getAttribute('href'));
    expect(hrefs.some((h) => h?.includes('/ideas/i1'))).toBeTrue();
    expect(hrefs.some((h) => h?.includes('/tracks/t1'))).toBeTrue();
  });

  it('does not call the API when there is no query', async () => {
    await setup(null);
    expect(api.lastQuery).toBe('');
  });

  it('shows a "no results" empty state naming the query when the search returns nothing', async () => {
    await setup('zzz');
    api.result = { ideas: [], tracks: [] };
    fixture.componentInstance.query.set('zzz');
    fixture.componentInstance.reload();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('No results found.');
    expect(text).toContain('No results for "zzz".');
  });

  it('shows an error state with retry when the search call fails, and recovers on retry', async () => {
    api = new StubSearchApi();
    spyOn(api, 'search').and.returnValue(Promise.reject(new Error('boom')));
    await TestBed.configureTestingModule({
      imports: [SearchComponent],
      providers: [
        provideRouter([]),
        { provide: PublicSearchApiService, useValue: api },
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: of(convertToParamMap({ q: 'solar' })) },
        },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(SearchComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    (api.search as jasmine.Spy).and.returnValue(Promise.resolve(api.result));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Solar Idea');
  });
});

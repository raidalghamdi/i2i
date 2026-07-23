import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { IdeasExplorerComponent } from './ideas-explorer.component';
import { IdeasApiService } from '../ideas-api.service';
import { StrategicThemesService } from '../strategic-themes.service';
import { ActivitiesService } from '../activities.service';
import { IdeaListItem, IdeaListPage } from '../idea.model';

describe('IdeasExplorerComponent', () => {
  let fixture: ComponentFixture<IdeasExplorerComponent>;
  let ideasApi: jasmine.SpyObj<IdeasApiService>;

  const item1: IdeaListItem = {
    id: 'i1',
    code: 'IDEA-001',
    titleAr: 'عنوان واحد',
    titleEn: 'Solar Rooftop Panels',
    problemStatementAr: 'مشكلة واحدة',
    problemStatementEn: 'Energy costs are too high',
    currentStage: 2,
    status: 'submitted',
    strategicThemeId: 't1',
    activityId: 'a1',
  };

  const item2: IdeaListItem = {
    id: 'i2',
    code: 'IDEA-002',
    titleAr: 'عنوان اثنين',
    titleEn: 'Water Recycling',
    problemStatementAr: 'مشكلة اثنين',
    problemStatementEn: 'Water waste in facilities',
    currentStage: 4,
    status: 'evaluation',
    strategicThemeId: 't2',
    activityId: 'a2',
  };

  const page: IdeaListPage = { items: [item1, item2], total: 2, page: 1, pageSize: 20 };

  async function setup(queryParams: Record<string, string> = {}): Promise<void> {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['list']);
    ideasApi.list.and.returnValue(Promise.resolve(page));

    const themesApi = { list: () => Promise.resolve([]) };
    const activitiesApi = { list: () => Promise.resolve([]) };

    await TestBed.configureTestingModule({
      imports: [IdeasExplorerComponent],
      providers: [
        provideRouter([{ path: 'ideas', children: [] }]),
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: StrategicThemesService, useValue: themesApi },
        { provide: ActivitiesService, useValue: activitiesApi },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap(queryParams) },
            queryParamMap: of(convertToParamMap(queryParams)),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(IdeasExplorerComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();
  }

  it('renders a card for each idea returned from the API', async () => {
    await setup();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('IDEA-001');
    expect(text).toContain('Solar Rooftop Panels');
    expect(text).toContain('IDEA-002');
    expect(text).toContain('Water Recycling');
  });

  it('links each card to /ideas/:id', async () => {
    await setup();
    const hrefs = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('a')).map((a) =>
      a.getAttribute('href'),
    );
    expect(hrefs.some((h) => h?.includes('/ideas/i1'))).toBeTrue();
    expect(hrefs.some((h) => h?.includes('/ideas/i2'))).toBeTrue();
  });

  it('re-lists with the selected status when the status filter changes', async () => {
    await setup();
    ideasApi.list.calls.reset();

    fixture.componentInstance.status.set('evaluation');
    await fixture.componentInstance.onFilterChange();

    expect(ideasApi.list).toHaveBeenCalledWith(jasmine.objectContaining({ status: 'evaluation' }));
  });

  it('debounces rapid search input into a single re-list call', async () => {
    await setup();
    ideasApi.list.calls.reset();

    fixture.componentInstance.onSearchInput('s');
    fixture.componentInstance.onSearchInput('so');
    fixture.componentInstance.onSearchInput('solar');

    expect(ideasApi.list).not.toHaveBeenCalled();

    await new Promise((resolve) => setTimeout(resolve, 350));

    expect(ideasApi.list).toHaveBeenCalledTimes(1);
    expect(ideasApi.list).toHaveBeenCalledWith(jasmine.objectContaining({ q: 'solar' }));
  });

  it('replaces the browser history entry when filters change', async () => {
    await setup();
    const router = TestBed.inject(Router);
    const navigateSpy = spyOn(router, 'navigate').and.callThrough();

    await fixture.componentInstance.onFilterChange();

    expect(navigateSpy).toHaveBeenCalledWith(jasmine.any(Array), jasmine.objectContaining({ replaceUrl: true }));
  });

  it('seeds the stage select with the numeric value from a deep-linked ?stage=3 query param', async () => {
    await setup({ stage: '3' });

    expect(fixture.componentInstance.stage()).toBe(3);

    const selects = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('select'));
    const stageSelect = selects[selects.length - 1] as HTMLSelectElement;
    expect(stageSelect.value).toBe('3');
  });

  it('shows an empty-state message when there are no results', async () => {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['list']);
    ideasApi.list.and.returnValue(Promise.resolve({ items: [], total: 0, page: 1, pageSize: 20 }));

    await TestBed.configureTestingModule({
      imports: [IdeasExplorerComponent],
      providers: [
        provideRouter([{ path: 'ideas', children: [] }]),
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: StrategicThemesService, useValue: { list: () => Promise.resolve([]) } },
        { provide: ActivitiesService, useValue: { list: () => Promise.resolve([]) } },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap({}) },
            queryParamMap: of(convertToParamMap({})),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(IdeasExplorerComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No ideas match');
  });

  it('renders the error state and retries the fetch when the "Try again" button is clicked', async () => {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['list']);
    ideasApi.list.and.returnValue(Promise.reject(new Error('network error')));

    await TestBed.configureTestingModule({
      imports: [IdeasExplorerComponent],
      providers: [
        provideRouter([{ path: 'ideas', children: [] }]),
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: StrategicThemesService, useValue: { list: () => Promise.resolve([]) } },
        { provide: ActivitiesService, useValue: { list: () => Promise.resolve([]) } },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap({}) },
            queryParamMap: of(convertToParamMap({})),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(IdeasExplorerComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const retryButton = el.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();

    ideasApi.list.and.returnValue(Promise.resolve(page));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBeNull();
    expect(el.textContent).toContain('IDEA-001');
  });
});

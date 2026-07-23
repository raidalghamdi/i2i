import { ComponentFixture, TestBed } from '@angular/core/testing';
import { IdeasApiService } from '../../ideas/ideas-api.service';
import { StrategicThemesService } from '../../ideas/strategic-themes.service';
import { ActivitiesService } from '../../ideas/activities.service';
import { SupervisorApiService } from '../../supervisor/supervisor-api.service';
import { IdeaListPage } from '../../ideas/idea.model';
import { AllIdeasConsoleComponent } from './all-ideas-console.component';

describe('AllIdeasConsoleComponent', () => {
  let fixture: ComponentFixture<AllIdeasConsoleComponent>;
  let ideasApi: jasmine.SpyObj<IdeasApiService>;
  let themesApi: jasmine.SpyObj<StrategicThemesService>;
  let activitiesApi: jasmine.SpyObj<ActivitiesService>;
  let supervisorApi: jasmine.SpyObj<SupervisorApiService>;

  const page: IdeaListPage = {
    items: [
      { id: 'i1', code: 'IDEA-0001', titleAr: 'أ', titleEn: 'Alpha', problemStatementAr: 'ب', problemStatementEn: 'Prob A', currentStage: 1, status: 'submitted', strategicThemeId: 't1', activityId: null },
      { id: 'i2', code: 'IDEA-0002', titleAr: 'ج', titleEn: 'Beta', problemStatementAr: 'د', problemStatementEn: 'Prob B', currentStage: 2, status: 'evaluation', strategicThemeId: 't1', activityId: 'a1' },
    ],
    total: 2, page: 1, pageSize: 20,
  };

  function setup(): void {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['list']);
    themesApi = jasmine.createSpyObj('StrategicThemesService', ['list']);
    activitiesApi = jasmine.createSpyObj('ActivitiesService', ['list']);
    ideasApi.list.and.resolveTo(page);
    themesApi.list.and.resolveTo([{ id: 't1', nameAr: 'موضوع', nameEn: 'Theme One' } as any]);
    activitiesApi.list.and.resolveTo([{ id: 'a1', nameAr: 'نشاط', nameEn: 'Activity One' } as any]);
    supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['submitScreeningDecision']);
    supervisorApi.submitScreeningDecision.and.resolveTo({} as any);

    TestBed.configureTestingModule({
      imports: [AllIdeasConsoleComponent],
      providers: [
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: StrategicThemesService, useValue: themesApi },
        { provide: ActivitiesService, useValue: activitiesApi },
        { provide: SupervisorApiService, useValue: supervisorApi },
      ],
    });
    fixture = TestBed.createComponent(AllIdeasConsoleComponent);
  }

  it('loads and lists every returned idea', async () => {
    setup();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('IDEA-0001');
    expect(text).toContain('IDEA-0002');
  });

  it('passes filters to the api when searching', async () => {
    setup();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.componentInstance.filterForm.setValue({ q: 'alpha', strategicThemeId: 't1', activityId: '', status: 'submitted' });
    await fixture.componentInstance.applyFilters();
    const lastArg = ideasApi.list.calls.mostRecent().args[0];
    expect(lastArg.q).toBe('alpha');
    expect(lastArg.strategicThemeId).toBe('t1');
    expect(lastArg.status).toBe('submitted');
    expect(lastArg.activityId).toBeUndefined();
  });

  it('submits an approve decision for a submitted idea and reloads', async () => {
    setup();
    fixture.detectChanges();
    await fixture.whenStable();
    await fixture.componentInstance.decide('i1', 'approve');
    expect(supervisorApi.submitScreeningDecision).toHaveBeenCalledWith('i1', { decisionCode: 'approve', reason: null });
    expect(ideasApi.list).toHaveBeenCalledTimes(2); // initial + reload
  });

  it('requires a reason before submitting a reject', async () => {
    setup();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.componentInstance.decisionReason.set('');
    await fixture.componentInstance.decide('i1', 'reject');
    expect(supervisorApi.submitScreeningDecision).not.toHaveBeenCalled();
    expect(fixture.componentInstance.decisionError()).toBeTruthy();
  });

  it('does not leak a reason from one row into an approve on another row', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    // user starts a return on idea i1
    fixture.componentInstance.toggle('i1');
    fixture.componentInstance.decisionReason.set('this is a long enough return reason');
    // switches to idea i2 and approves
    fixture.componentInstance.toggle('i2');
    await fixture.componentInstance.decide('i2', 'approve');
    expect(supervisorApi.submitScreeningDecision).toHaveBeenCalledWith('i2', { decisionCode: 'approve', reason: null });
  });

  it('shows an empty-state message when no ideas match the filters', async () => {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['list']);
    ideasApi.list.and.resolveTo({ items: [], total: 0, page: 1, pageSize: 100 });
    themesApi = jasmine.createSpyObj('StrategicThemesService', ['list']);
    themesApi.list.and.resolveTo([]);
    activitiesApi = jasmine.createSpyObj('ActivitiesService', ['list']);
    activitiesApi.list.and.resolveTo([]);
    supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['submitScreeningDecision']);

    TestBed.configureTestingModule({
      imports: [AllIdeasConsoleComponent],
      providers: [
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: StrategicThemesService, useValue: themesApi },
        { provide: ActivitiesService, useValue: activitiesApi },
        { provide: SupervisorApiService, useValue: supervisorApi },
      ],
    });
    fixture = TestBed.createComponent(AllIdeasConsoleComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No ideas match these filters');
  });

  it('renders the error state and retries the fetch when the "Try again" button is clicked', async () => {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['list']);
    ideasApi.list.and.returnValue(Promise.reject(new Error('network error')));
    themesApi = jasmine.createSpyObj('StrategicThemesService', ['list']);
    themesApi.list.and.resolveTo([]);
    activitiesApi = jasmine.createSpyObj('ActivitiesService', ['list']);
    activitiesApi.list.and.resolveTo([]);
    supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['submitScreeningDecision']);

    TestBed.configureTestingModule({
      imports: [AllIdeasConsoleComponent],
      providers: [
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: StrategicThemesService, useValue: themesApi },
        { provide: ActivitiesService, useValue: activitiesApi },
        { provide: SupervisorApiService, useValue: supervisorApi },
      ],
    });
    fixture = TestBed.createComponent(AllIdeasConsoleComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const retryButton = el.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();

    ideasApi.list.and.resolveTo(page);
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBeNull();
    expect(el.textContent).toContain('IDEA-0001');
  });
});

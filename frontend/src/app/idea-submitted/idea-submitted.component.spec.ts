import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { IdeasApiService } from '../ideas/ideas-api.service';
import { Idea } from '../ideas/idea.model';
import { IdeaSubmittedComponent } from './idea-submitted.component';

describe('IdeaSubmittedComponent', () => {
  let fixture: ComponentFixture<IdeaSubmittedComponent>;
  let ideasApi: jasmine.SpyObj<IdeasApiService>;

  const idea: Idea = {
    id: 'idea-1', code: 'IDEA-0001', submitterId: 'user-1', titleAr: 'ا', titleEn: 'My Great Idea',
    problemStatementAr: 'م', problemStatementEn: 'Problem', proposedSolutionAr: 'ح', proposedSolutionEn: 'Solution',
    expectedBenefitsAr: 'ف', expectedBenefitsEn: 'Benefits', strategicThemeId: 'theme-1',
    activityId: 'activity-1', challengeId: null, participationType: 'individual', teamName: null, teamMembers: [],
    ipAcknowledged: true, termsAgreed: true,
    status: 'submitted', currentStage: 1, updatedAt: '2026-01-01', attachments: [], screeningReason: null,
  };

  function setup(): void {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getById']);
    ideasApi.getById.and.returnValue(Promise.resolve(idea));

    TestBed.configureTestingModule({
      imports: [IdeaSubmittedComponent],
      providers: [
        provideRouter([]),
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
    fixture = TestBed.createComponent(IdeaSubmittedComponent);
  }

  it('shows the submitted idea title and code', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('My Great Idea');
    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
  });

  it('links to the idea detail page and the dashboard', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.some((a) => a.getAttribute('href') === '/ideas/idea-1')).toBe(true);
    expect(links.some((a) => a.getAttribute('href') === '/dashboard')).toBe(true);
  });

  it('shows a loading state before the idea resolves', () => {
    const pendingApi = jasmine.createSpyObj('IdeasApiService', ['getById']);
    pendingApi.getById.and.returnValue(new Promise(() => {}));

    TestBed.configureTestingModule({
      imports: [IdeaSubmittedComponent],
      providers: [
        provideRouter([]),
        { provide: IdeasApiService, useValue: pendingApi },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
    const pendingFixture = TestBed.createComponent(IdeaSubmittedComponent);
    pendingFixture.detectChanges();

    expect(pendingFixture.nativeElement.querySelector('app-loading-state')).toBeTruthy();
  });

  it('redirects to the idea detail page when getById fails (e.g. not the owner)', async () => {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getById']);
    ideasApi.getById.and.returnValue(Promise.reject({ status: 403 }));
    const router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [IdeaSubmittedComponent],
      providers: [
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
    const failFixture = TestBed.createComponent(IdeaSubmittedComponent);
    failFixture.detectChanges();
    await failFixture.componentInstance.ngOnInit();

    expect(router.navigate).toHaveBeenCalledWith(['/ideas', 'idea-1']);
  });
});

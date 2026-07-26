import { ComponentFixture, TestBed } from '@angular/core/testing'; // Change 20260726
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router'; // Change 20260726
import { HttpErrorResponse } from '@angular/common/http'; // Change 20260726
import { IdeaDetailViewComponent } from './idea-detail.component'; // Change 20260726
import { IdeaDetailView, IdeasService } from '../ideas.service'; // Change 20260726
import { StrategicThemesService } from '../strategic-themes.service'; // Change 20260726

function detail(overrides: Partial<IdeaDetailView> = {}): IdeaDetailView { // Change 20260726
  return { // Change 20260726
    id: 'i1', // Change 20260726
    code: 'IDEA-001', // Change 20260726
    submitterId: 'u1', // Change 20260726
    titleAr: 'ألواح شمسية', // Change 20260726
    titleEn: 'Solar Rooftop Panels', // Change 20260726
    problemStatementAr: 'المشكلة', // Change 20260726
    problemStatementEn: 'Rooftops are unused', // Change 20260726
    proposedSolutionAr: 'الحل', // Change 20260726
    proposedSolutionEn: 'Install panels', // Change 20260726
    expectedBenefitsAr: 'الفوائد', // Change 20260726
    expectedBenefitsEn: 'Lower bills', // Change 20260726
    strategicThemeId: 't1', // Change 20260726
    activityId: 'a1', // Change 20260726
    challengeId: null, // Change 20260726
    participationType: 'individual', // Change 20260726
    teamName: null, // Change 20260726
    teamMembers: [], // Change 20260726
    ipAcknowledged: true, // Change 20260726
    termsAgreed: true, // Change 20260726
    status: 'submitted', // Change 20260726
    currentStage: 1, // Change 20260726
    createdAt: '2026-01-01T00:00:00Z', // Change 20260726
    updatedAt: '2026-01-05T00:00:00Z', // Change 20260726
    approvedAt: null, // Change 20260726
    attachments: [], // Change 20260726
    screeningReason: null, // Change 20260726
    editableSections: null, // Change 20260726
    auditTrail: [], // Change 20260726
    ...overrides, // Change 20260726
  } as IdeaDetailView; // Change 20260726
} // Change 20260726

describe('IdeaDetailViewComponent', () => { // Change 20260726
  let fixture: ComponentFixture<IdeaDetailViewComponent>; // Change 20260726
  let ideas: jasmine.SpyObj<IdeasService>; // Change 20260726
  let themes: jasmine.SpyObj<StrategicThemesService>; // Change 20260726

  beforeEach(() => { // Change 20260726
    ideas = jasmine.createSpyObj<IdeasService>('IdeasService', ['getMine', 'getById']); // Change 20260726
    themes = jasmine.createSpyObj<StrategicThemesService>('StrategicThemesService', ['list']); // Change 20260726
    themes.list.and.resolveTo([ // Change 20260726
      { id: 't1', nameAr: 'الطاقة', nameEn: 'Energy', descriptionAr: '', descriptionEn: '' }, // Change 20260726
    ] as never); // Change 20260726
    TestBed.configureTestingModule({ // Change 20260726
      imports: [IdeaDetailViewComponent], // Change 20260726
      providers: [ // Change 20260726
        provideRouter([]), // Change 20260726
        { provide: IdeasService, useValue: ideas }, // Change 20260726
        { provide: StrategicThemesService, useValue: themes }, // Change 20260726
        { // Change 20260726
          provide: ActivatedRoute, // Change 20260726
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'i1' }) } }, // Change 20260726
        }, // Change 20260726
      ], // Change 20260726
    }); // Change 20260726
  }); // Change 20260726

  async function render(): Promise<void> { // Change 20260726
    fixture = TestBed.createComponent(IdeaDetailViewComponent); // Change 20260726
    fixture.detectChanges(); // Change 20260726
    await fixture.whenStable(); // Change 20260726
    fixture.detectChanges(); // Change 20260726
  } // Change 20260726

  function text(): string { // Change 20260726
    return (fixture.nativeElement as HTMLElement).textContent ?? ''; // Change 20260726
  } // Change 20260726

  it('renders the idea once the service resolves', async () => { // Change 20260726
    ideas.getById.and.resolveTo(detail()); // Change 20260726
    await render(); // Change 20260726

    expect(text()).toContain('IDEA-001'); // Change 20260726
    expect(text()).toContain('Solar Rooftop Panels'); // Change 20260726
    expect(text()).toContain('ألواح شمسية'); // Change 20260726
    expect(text()).toContain('Energy'); // Change 20260726
  }); // Change 20260726

  it('shows "No attachments" when the idea has none', async () => { // Change 20260726
    ideas.getById.and.resolveTo(detail()); // Change 20260726
    await render(); // Change 20260726

    expect(text()).toContain('No attachments'); // Change 20260726
  }); // Change 20260726

  it('lists attachments with size and upload date', async () => { // Change 20260726
    ideas.getById.and.resolveTo( // Change 20260726
      detail({ // Change 20260726
        attachments: [ // Change 20260726
          { // Change 20260726
            id: 'f1', // Change 20260726
            fileName: 'plan.pdf', // Change 20260726
            contentType: 'application/pdf', // Change 20260726
            fileSizeBytes: 2048, // Change 20260726
            uploadedAt: '2026-01-03T00:00:00Z', // Change 20260726
          }, // Change 20260726
        ], // Change 20260726
      }), // Change 20260726
    ); // Change 20260726
    await render(); // Change 20260726

    expect(text()).toContain('plan.pdf'); // Change 20260726
    expect(text()).toContain('2 KB'); // Change 20260726
  }); // Change 20260726

  it('hides the feedback banner for statuses that are not returned or needs_completion', async () => { // Change 20260726
    ideas.getById.and.resolveTo(detail({ status: 'submitted' })); // Change 20260726
    await render(); // Change 20260726

    expect((fixture.nativeElement as HTMLElement).querySelector('[data-testid="detail-feedback"]')).toBeNull(); // Change 20260726
  }); // Change 20260726

  it('shows the feedback banner when the idea was returned', async () => { // Change 20260726
    ideas.getById.and.resolveTo(detail({ status: 'returned', screeningReason: 'Add a cost estimate' })); // Change 20260726
    await render(); // Change 20260726

    const banner = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="detail-feedback"]'); // Change 20260726
    expect(banner?.textContent).toContain('Add a cost estimate'); // Change 20260726
    expect(banner?.textContent).toContain('Edit and Resubmit'); // Change 20260726
  }); // Change 20260726

  it('shows the feedback banner when the idea needs completion', async () => { // Change 20260726
    ideas.getById.and.resolveTo(detail({ status: 'needs_completion' })); // Change 20260726
    await render(); // Change 20260726

    expect((fixture.nativeElement as HTMLElement).querySelector('[data-testid="detail-feedback"]')).toBeTruthy(); // Change 20260726
  }); // Change 20260726

  it('renders the audit trail as a timeline of transitions', async () => { // Change 20260726
    ideas.getById.and.resolveTo( // Change 20260726
      detail({ // Change 20260726
        auditTrail: [ // Change 20260726
          { // Change 20260726
            action: 'idea.submitted', // Change 20260726
            actorId: 'u1', // Change 20260726
            occurredAt: '2026-01-02T00:00:00Z', // Change 20260726
            payload: '{"fromStatus":"draft","toStatus":"submitted","notes":"Initial submission"}', // Change 20260726
          }, // Change 20260726
        ], // Change 20260726
      }), // Change 20260726
    ); // Change 20260726
    await render(); // Change 20260726

    const rows = fixture.componentInstance.timeline(); // Change 20260726
    expect(rows.length).toBe(1); // Change 20260726
    expect(rows[0].fromStatus).toBe('draft'); // Change 20260726
    expect(rows[0].toStatus).toBe('submitted'); // Change 20260726
    expect(text()).toContain('Initial submission'); // Change 20260726
  }); // Change 20260726

  it('tolerates audit payloads that are not JSON', async () => { // Change 20260726
    ideas.getById.and.resolveTo( // Change 20260726
      detail({ // Change 20260726
        auditTrail: [{ action: 'idea.created', actorId: null, occurredAt: '2026-01-01T00:00:00Z', payload: 'not-json' }], // Change 20260726
      }), // Change 20260726
    ); // Change 20260726
    await render(); // Change 20260726

    expect(fixture.componentInstance.timeline()[0].toStatus).toBeNull(); // Change 20260726
    expect(text()).toContain('idea.created'); // Change 20260726
  }); // Change 20260726

  it('counts resubmissions as revisions', async () => { // Change 20260726
    ideas.getById.and.resolveTo( // Change 20260726
      detail({ // Change 20260726
        auditTrail: [ // Change 20260726
          { action: 'idea.resubmitted', actorId: 'u1', occurredAt: '2026-01-03T00:00:00Z', payload: null }, // Change 20260726
          { action: 'idea.returned', actorId: 'u2', occurredAt: '2026-01-04T00:00:00Z', payload: null }, // Change 20260726
        ], // Change 20260726
      }), // Change 20260726
    ); // Change 20260726
    await render(); // Change 20260726

    expect(fixture.componentInstance.revisionCount()).toBe(1); // Change 20260726
    expect((fixture.nativeElement as HTMLElement).querySelector('[data-testid="detail-revisions"]')).toBeTruthy(); // Change 20260726
  }); // Change 20260726

  it('renders the not-found state on a 404', async () => { // Change 20260726
    ideas.getById.and.rejectWith(new HttpErrorResponse({ status: 404 })); // Change 20260726
    await render(); // Change 20260726

    expect(text()).toContain("Idea not found or you don't have access"); // Change 20260726
  }); // Change 20260726

  it('renders the error banner on other failures', async () => { // Change 20260726
    ideas.getById.and.rejectWith(new HttpErrorResponse({ status: 500 })); // Change 20260726
    await render(); // Change 20260726

    expect((fixture.nativeElement as HTMLElement).querySelector('[data-testid="detail-error"]')).toBeTruthy(); // Change 20260726
  }); // Change 20260726
}); // Change 20260726

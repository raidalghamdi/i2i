import { ComponentFixture, TestBed } from '@angular/core/testing'; // Change 20260726
import { ActivatedRoute, Router, provideRouter } from '@angular/router'; // Change 20260726
import { IdeaEditComponent, parseEditableSections } from './idea-edit.component'; // Change 20260726
import { IdeaDetailView, IdeasService } from '../ideas.service'; // Change 20260726
import { StrategicTheme } from '../idea.model'; // Change 20260726

const THEMES: StrategicTheme[] = [{ id: 't1', nameAr: 'الطاقة', nameEn: 'Energy' }]; // Change 20260726

function idea(overrides: Partial<IdeaDetailView> = {}): IdeaDetailView { // Change 20260726
  return { // Change 20260726
    id: 'idea-1', // Change 20260726
    code: 'IDEA-001', // Change 20260726
    titleAr: 'ألواح شمسية', // Change 20260726
    titleEn: 'Solar Rooftop Panels', // Change 20260726
    problemStatementAr: 'مشكلة', // Change 20260726
    problemStatementEn: 'Depot roofs are unused.', // Change 20260726
    proposedSolutionAr: '', // Change 20260726
    proposedSolutionEn: '', // Change 20260726
    expectedBenefitsAr: '', // Change 20260726
    expectedBenefitsEn: '', // Change 20260726
    strategicThemeId: 't1', // Change 20260726
    activityId: 'a1', // Change 20260726
    challengeId: null, // Change 20260726
    participationType: 'individual', // Change 20260726
    teamName: null, // Change 20260726
    teamMembers: [], // Change 20260726
    ipAcknowledged: true, // Change 20260726
    termsAgreed: true, // Change 20260726
    submitterId: 'u1', // Change 20260726
    status: 'draft', // Change 20260726
    currentStage: 0, // Change 20260726
    createdAt: '2026-07-01T00:00:00Z', // Change 20260726
    updatedAt: '2026-07-02T00:00:00Z', // Change 20260726
    approvedAt: null, // Change 20260726
    attachments: [], // Change 20260726
    screeningReason: null, // Change 20260726
    editableSections: null, // Change 20260726
    auditTrail: [], // Change 20260726
    ...overrides, // Change 20260726
  }; // Change 20260726
} // Change 20260726

describe('IdeaEditComponent', () => { // Change 20260726
  let fixture: ComponentFixture<IdeaEditComponent>; // Change 20260726
  let component: IdeaEditComponent; // Change 20260726
  let ideas: jasmine.SpyObj<IdeasService>; // Change 20260726
  let navigate: jasmine.Spy; // Change 20260726

  async function render(): Promise<void> { // Change 20260726
    fixture = TestBed.createComponent(IdeaEditComponent); // Change 20260726
    component = fixture.componentInstance; // Change 20260726
    fixture.detectChanges(); // Change 20260726
    // ngOnInit awaits getById and then getStrategicThemes, so the template has to be // Change 20260726
    // refreshed after each of those settles before the form markup exists. // Change 20260726
    for (let pass = 0; pass < 3; pass++) { // Change 20260726
      await fixture.whenStable(); // Change 20260726
      fixture.detectChanges(); // Change 20260726
    } // Change 20260726
  } // Change 20260726

  beforeEach(() => { // Change 20260726
    ideas = jasmine.createSpyObj<IdeasService>('IdeasService', [ // Change 20260726
      'getById', // Change 20260726
      'getStrategicThemes', // Change 20260726
      'updateIdea', // Change 20260726
      'submitIdea', // Change 20260726
      'addAttachment', // Change 20260726
      'deleteAttachment', // Change 20260726
    ]); // Change 20260726
    ideas.getById.and.resolveTo(idea()); // Change 20260726
    ideas.getStrategicThemes.and.resolveTo(THEMES); // Change 20260726
    ideas.updateIdea.and.resolveTo({ id: 'idea-1', code: 'IDEA-001' }); // Change 20260726
    ideas.submitIdea.and.resolveTo({ id: 'idea-1', status: 'submitted' }); // Change 20260726
    ideas.deleteAttachment.and.resolveTo(undefined); // Change 20260726

    TestBed.configureTestingModule({ // Change 20260726
      imports: [IdeaEditComponent], // Change 20260726
      providers: [ // Change 20260726
        provideRouter([]), // Change 20260726
        { provide: IdeasService, useValue: ideas }, // Change 20260726
        { // Change 20260726
          provide: ActivatedRoute, // Change 20260726
          useValue: { snapshot: { paramMap: new Map([['id', 'idea-1']]) } }, // Change 20260726
        }, // Change 20260726
      ], // Change 20260726
    }); // Change 20260726
    navigate = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true); // Change 20260726
  }); // Change 20260726

  it('pre-populates the form from the fetched idea', async () => { // Change 20260726
    await render(); // Change 20260726

    expect(ideas.getById).toHaveBeenCalledWith('idea-1'); // Change 20260726
    expect(component.form.getRawValue()).toEqual({ // Change 20260726
      titleEn: 'Solar Rooftop Panels', // Change 20260726
      titleAr: 'ألواح شمسية', // Change 20260726
      descriptionEn: 'Depot roofs are unused.', // Change 20260726
      descriptionAr: 'مشكلة', // Change 20260726
      strategicThemeId: 't1', // Change 20260726
    }); // Change 20260726
  }); // Change 20260726

  it('blocks editing an idea past the editable statuses', async () => { // Change 20260726
    ideas.getById.and.resolveTo(idea({ status: 'approved' })); // Change 20260726
    await render(); // Change 20260726

    expect(component.blocked()).toBeTruthy(); // Change 20260726
    const blocked = (fixture.nativeElement as HTMLElement).querySelector( // Change 20260726
      '[data-testid="idea-edit-blocked"]', // Change 20260726
    ); // Change 20260726
    expect(blocked).toBeTruthy(); // Change 20260726
    expect(blocked?.querySelector('a')?.getAttribute('href')).toBe('/ideas/mine'); // Change 20260726
    expect(ideas.updateIdea).not.toHaveBeenCalled(); // Change 20260726
  }); // Change 20260726

  it('blocks editing an idea owned by someone else', async () => { // Change 20260726
    ideas.getById.and.rejectWith({ status: 403 }); // Change 20260726
    await render(); // Change 20260726

    expect(component.blocked()).toBe('You can only edit ideas you submitted.'); // Change 20260726
  }); // Change 20260726

  it('offers Save Draft only while the idea is a draft', async () => { // Change 20260726
    await render(); // Change 20260726
    expect( // Change 20260726
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="idea-save-draft"]'), // Change 20260726
    ).toBeTruthy(); // Change 20260726

    ideas.getById.and.resolveTo(idea({ status: 'returned' })); // Change 20260726
    await render(); // Change 20260726

    expect(component.canSaveDraft()).toBeFalse(); // Change 20260726
    expect( // Change 20260726
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="idea-save-draft"]'), // Change 20260726
    ).toBeNull(); // Change 20260726
  }); // Change 20260726

  it('disables fields outside editableSections when the idea needs completion', async () => { // Change 20260726
    ideas.getById.and.resolveTo( // Change 20260726
      idea({ status: 'needs_completion', editableSections: '["titleEn","descriptionEn"]' }), // Change 20260726
    ); // Change 20260726
    await render(); // Change 20260726

    expect(component.isRestricted()).toBeTrue(); // Change 20260726
    expect(component.form.controls.titleEn.enabled).toBeTrue(); // Change 20260726
    expect(component.form.controls.descriptionEn.enabled).toBeTrue(); // Change 20260726
    expect(component.form.controls.titleAr.disabled).toBeTrue(); // Change 20260726
    expect(component.form.controls.descriptionAr.disabled).toBeTrue(); // Change 20260726
    expect(component.form.controls.strategicThemeId.disabled).toBeTrue(); // Change 20260726
    expect( // Change 20260726
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="idea-edit-restricted"]'), // Change 20260726
    ).toBeTruthy(); // Change 20260726
  }); // Change 20260726

  it('leaves every field editable when editableSections is empty or unparseable', async () => { // Change 20260726
    ideas.getById.and.resolveTo(idea({ status: 'needs_completion', editableSections: 'not json' })); // Change 20260726
    await render(); // Change 20260726

    expect(component.isRestricted()).toBeFalse(); // Change 20260726
    expect(component.form.controls.titleAr.enabled).toBeTrue(); // Change 20260726
  }); // Change 20260726

  it('sends locked fields back unchanged when saving a restricted idea', async () => { // Change 20260726
    ideas.getById.and.resolveTo( // Change 20260726
      idea({ status: 'needs_completion', editableSections: '["titleEn"]' }), // Change 20260726
    ); // Change 20260726
    await render(); // Change 20260726

    component.form.controls.titleEn.setValue('Revised title'); // Change 20260726
    await component.confirmResubmit(); // Change 20260726

    expect(ideas.updateIdea).toHaveBeenCalledWith('idea-1', { // Change 20260726
      titleEn: 'Revised title', // Change 20260726
      titleAr: 'ألواح شمسية', // Change 20260726
      descriptionEn: 'Depot roofs are unused.', // Change 20260726
      descriptionAr: 'مشكلة', // Change 20260726
      strategicThemeId: 't1', // Change 20260726
    } as never); // Change 20260726
  }); // Change 20260726

  it('saving a draft updates the idea without submitting it', async () => { // Change 20260726
    await render(); // Change 20260726

    await component.saveDraft(); // Change 20260726

    expect(ideas.updateIdea).toHaveBeenCalled(); // Change 20260726
    expect(ideas.submitIdea).not.toHaveBeenCalled(); // Change 20260726
    expect(navigate).toHaveBeenCalledWith(['/ideas/mine']); // Change 20260726
  }); // Change 20260726

  it('confirming a resubmit updates then submits', async () => { // Change 20260726
    await render(); // Change 20260726

    component.requestResubmit(); // Change 20260726
    fixture.detectChanges(); // Change 20260726
    expect(component.confirming()).toBeTrue(); // Change 20260726

    await component.confirmResubmit(); // Change 20260726

    expect(ideas.updateIdea).toHaveBeenCalledBefore(ideas.submitIdea); // Change 20260726
    expect(ideas.submitIdea).toHaveBeenCalledWith('idea-1'); // Change 20260726
    expect(navigate).toHaveBeenCalledWith(['/ideas/mine']); // Change 20260726
  }); // Change 20260726

  it('cancelling the resubmit confirmation changes nothing', async () => { // Change 20260726
    await render(); // Change 20260726

    component.requestResubmit(); // Change 20260726
    component.cancelResubmit(); // Change 20260726

    expect(component.confirming()).toBeFalse(); // Change 20260726
    expect(ideas.updateIdea).not.toHaveBeenCalled(); // Change 20260726
  }); // Change 20260726

  it('blocks resubmitting until the full field set is present', async () => { // Change 20260726
    ideas.getById.and.resolveTo(idea({ titleAr: '' })); // Change 20260726
    await render(); // Change 20260726

    component.requestResubmit(); // Change 20260726
    fixture.detectChanges(); // Change 20260726

    expect(component.confirming()).toBeFalse(); // Change 20260726
    expect(ideas.updateIdea).not.toHaveBeenCalled(); // Change 20260726
    expect( // Change 20260726
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="idea-edit-submit-invalid"]'), // Change 20260726
    ).toBeTruthy(); // Change 20260726
  }); // Change 20260726

  it('shows a red banner with the backend message when the PUT fails', async () => { // Change 20260726
    ideas.updateIdea.and.rejectWith({ error: { error: 'Idea is not in draft state.' } }); // Change 20260726
    await render(); // Change 20260726

    await component.saveDraft(); // Change 20260726
    fixture.detectChanges(); // Change 20260726

    expect(component.error()).toBe('Idea is not in draft state.'); // Change 20260726
    expect(navigate).not.toHaveBeenCalled(); // Change 20260726
    expect( // Change 20260726
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="idea-edit-error"]')?.textContent, // Change 20260726
    ).toContain('Idea is not in draft state.'); // Change 20260726
  }); // Change 20260726

  it('deletes an existing attachment and drops it from the list', async () => { // Change 20260726
    const attachment = { // Change 20260726
      id: 'a1', // Change 20260726
      fileName: 'plan.pdf', // Change 20260726
      contentType: 'application/pdf', // Change 20260726
      fileSizeBytes: 10, // Change 20260726
      uploadedAt: '2026-07-01T00:00:00Z', // Change 20260726
    }; // Change 20260726
    ideas.getById.and.resolveTo(idea({ attachments: [attachment] })); // Change 20260726
    await render(); // Change 20260726
    expect(component.existingAttachments().length).toBe(1); // Change 20260726

    await component.deleteExistingAttachment(attachment); // Change 20260726

    expect(ideas.deleteAttachment).toHaveBeenCalledWith('idea-1', 'a1'); // Change 20260726
    expect(component.existingAttachments()).toEqual([]); // Change 20260726
  }); // Change 20260726

  it('queues newly picked files and uploads them on save', async () => { // Change 20260726
    ideas.addAttachment.and.resolveTo({ // Change 20260726
      id: 'a2', // Change 20260726
      fileName: 'extra.pdf', // Change 20260726
      contentType: 'application/pdf', // Change 20260726
      fileSizeBytes: 2, // Change 20260726
      uploadedAt: '2026-07-26T00:00:00Z', // Change 20260726
    }); // Change 20260726
    await render(); // Change 20260726
    const file = new File(['x'], 'extra.pdf', { type: 'application/pdf' }); // Change 20260726

    component.onFilesSelected({ target: { files: [file], value: '' } } as unknown as Event); // Change 20260726
    expect(component.pendingUploads()).toEqual(['extra.pdf']); // Change 20260726

    await component.saveDraft(); // Change 20260726

    expect(ideas.addAttachment).toHaveBeenCalledWith('idea-1', file); // Change 20260726
  }); // Change 20260726

  it('removes a queued file before it is uploaded', async () => { // Change 20260726
    await render(); // Change 20260726
    const file = new File(['x'], 'extra.pdf', { type: 'application/pdf' }); // Change 20260726
    component.onFilesSelected({ target: { files: [file], value: '' } } as unknown as Event); // Change 20260726

    component.removePendingUpload(0); // Change 20260726
    await component.saveDraft(); // Change 20260726

    expect(component.pendingUploads()).toEqual([]); // Change 20260726
    expect(ideas.addAttachment).not.toHaveBeenCalled(); // Change 20260726
  }); // Change 20260726
}); // Change 20260726

describe('parseEditableSections', () => { // Change 20260726
  it('reads a JSON array of section keys', () => { // Change 20260726
    expect(parseEditableSections('["titleEn","titleAr"]')).toEqual(['titleEn', 'titleAr']); // Change 20260726
  }); // Change 20260726

  it('treats null, blank and malformed values as unrestricted', () => { // Change 20260726
    expect(parseEditableSections(null)).toEqual([]); // Change 20260726
    expect(parseEditableSections('  ')).toEqual([]); // Change 20260726
    expect(parseEditableSections('{}')).toEqual([]); // Change 20260726
    expect(parseEditableSections('nope')).toEqual([]); // Change 20260726
  }); // Change 20260726

  it('ignores non-string entries', () => { // Change 20260726
    expect(parseEditableSections('["titleEn",7,null]')).toEqual(['titleEn']); // Change 20260726
  }); // Change 20260726
}); // Change 20260726

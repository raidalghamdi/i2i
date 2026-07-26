import { ComponentFixture, TestBed } from '@angular/core/testing'; // Change 20260726
import { Router, provideRouter } from '@angular/router'; // Change 20260726
import { IdeaNewComponent } from './idea-new.component'; // Change 20260726
import { IdeasService } from '../ideas.service'; // Change 20260726
import { StrategicTheme } from '../idea.model'; // Change 20260726

const THEMES: StrategicTheme[] = [ // Change 20260726
  { id: 't1', nameAr: 'الطاقة', nameEn: 'Energy' }, // Change 20260726
  { id: 't2', nameAr: 'المياه', nameEn: 'Water' }, // Change 20260726
]; // Change 20260726

describe('IdeaNewComponent', () => { // Change 20260726
  let fixture: ComponentFixture<IdeaNewComponent>; // Change 20260726
  let component: IdeaNewComponent; // Change 20260726
  let ideas: jasmine.SpyObj<IdeasService>; // Change 20260726
  let navigate: jasmine.Spy; // Change 20260726

  async function render(): Promise<void> { // Change 20260726
    fixture = TestBed.createComponent(IdeaNewComponent); // Change 20260726
    component = fixture.componentInstance; // Change 20260726
    fixture.detectChanges(); // Change 20260726
    await fixture.whenStable(); // Change 20260726
    fixture.detectChanges(); // Change 20260726
  } // Change 20260726

  /** Fills every field the backend requires for a submit. */ // Change 20260726
  function fillComplete(): void { // Change 20260726
    component.form.setValue({ // Change 20260726
      titleEn: 'Solar Rooftop Panels', // Change 20260726
      titleAr: 'ألواح شمسية', // Change 20260726
      descriptionEn: 'Install panels on depot roofs.', // Change 20260726
      descriptionAr: 'تركيب الألواح', // Change 20260726
      strategicThemeId: 't1', // Change 20260726
    }); // Change 20260726
  } // Change 20260726

  beforeEach(() => { // Change 20260726
    ideas = jasmine.createSpyObj<IdeasService>('IdeasService', [ // Change 20260726
      'getStrategicThemes', // Change 20260726
      'createDraft', // Change 20260726
      'submitIdea', // Change 20260726
      'addAttachment', // Change 20260726
    ]); // Change 20260726
    ideas.getStrategicThemes.and.resolveTo(THEMES); // Change 20260726
    ideas.createDraft.and.resolveTo({ id: 'idea-1', code: 'IDEA-001', status: 'draft' }); // Change 20260726
    ideas.submitIdea.and.resolveTo({ id: 'idea-1', status: 'submitted' }); // Change 20260726

    TestBed.configureTestingModule({ // Change 20260726
      imports: [IdeaNewComponent], // Change 20260726
      providers: [provideRouter([]), { provide: IdeasService, useValue: ideas }], // Change 20260726
    }); // Change 20260726
    navigate = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true); // Change 20260726
  }); // Change 20260726

  it('loads strategic themes into the dropdown on init', async () => { // Change 20260726
    await render(); // Change 20260726

    expect(ideas.getStrategicThemes).toHaveBeenCalled(); // Change 20260726
    const options = (fixture.nativeElement as HTMLElement).querySelectorAll( // Change 20260726
      '[data-testid="idea-theme"] option', // Change 20260726
    ); // Change 20260726
    // Placeholder plus one option per theme. // Change 20260726
    expect(options.length).toBe(THEMES.length + 1); // Change 20260726
  }); // Change 20260726

  it('still allows drafting when the theme list fails to load', async () => { // Change 20260726
    ideas.getStrategicThemes.and.rejectWith(new Error('offline')); // Change 20260726
    await render(); // Change 20260726

    expect(component.themesFailed()).toBeTrue(); // Change 20260726
    const banner = (fixture.nativeElement as HTMLElement).querySelector( // Change 20260726
      '[data-testid="idea-new-themes-failed"]', // Change 20260726
    ); // Change 20260726
    expect(banner).toBeTruthy(); // Change 20260726

    component.form.patchValue({ titleEn: 'Draft only' }); // Change 20260726
    await component.saveDraft(); // Change 20260726

    expect(ideas.createDraft).toHaveBeenCalled(); // Change 20260726
  }); // Change 20260726

  it('saves a draft with only Title EN filled in and navigates to My Ideas', async () => { // Change 20260726
    await render(); // Change 20260726
    component.form.patchValue({ titleEn: 'Just a title' }); // Change 20260726

    await component.saveDraft(); // Change 20260726

    expect(ideas.createDraft).toHaveBeenCalled(); // Change 20260726
    expect(ideas.submitIdea).not.toHaveBeenCalled(); // Change 20260726
    expect(navigate).toHaveBeenCalledWith(['/ideas/mine']); // Change 20260726
  }); // Change 20260726

  it('refuses to save a draft without Title EN', async () => { // Change 20260726
    await render(); // Change 20260726

    await component.saveDraft(); // Change 20260726

    expect(ideas.createDraft).not.toHaveBeenCalled(); // Change 20260726
  }); // Change 20260726

  it('does not open the confirmation until every submit field is filled', async () => { // Change 20260726
    await render(); // Change 20260726
    component.form.patchValue({ titleEn: 'Only English title' }); // Change 20260726

    component.requestSubmit(); // Change 20260726
    fixture.detectChanges(); // Change 20260726

    expect(component.confirming()).toBeFalse(); // Change 20260726
    expect(ideas.createDraft).not.toHaveBeenCalled(); // Change 20260726
    expect( // Change 20260726
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="idea-new-submit-invalid"]'), // Change 20260726
    ).toBeTruthy(); // Change 20260726
  }); // Change 20260726

  it('confirming a submit creates the draft then submits it', async () => { // Change 20260726
    await render(); // Change 20260726
    fillComplete(); // Change 20260726

    component.requestSubmit(); // Change 20260726
    fixture.detectChanges(); // Change 20260726
    expect(component.confirming()).toBeTrue(); // Change 20260726
    expect( // Change 20260726
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="idea-confirm"]')?.textContent, // Change 20260726
    ).toContain("Once submitted, you can't edit unless returned."); // Change 20260726

    await component.confirmSubmit(); // Change 20260726

    expect(ideas.createDraft).toHaveBeenCalled(); // Change 20260726
    expect(ideas.submitIdea).toHaveBeenCalledWith('idea-1'); // Change 20260726
    expect(navigate).toHaveBeenCalledWith(['/ideas/mine']); // Change 20260726
  }); // Change 20260726

  it('cancelling the confirmation submits nothing', async () => { // Change 20260726
    await render(); // Change 20260726
    fillComplete(); // Change 20260726

    component.requestSubmit(); // Change 20260726
    component.cancelSubmit(); // Change 20260726

    expect(component.confirming()).toBeFalse(); // Change 20260726
    expect(ideas.createDraft).not.toHaveBeenCalled(); // Change 20260726
  }); // Change 20260726

  it('shows the backend error message and stays on the form when the POST fails', async () => { // Change 20260726
    ideas.createDraft.and.rejectWith({ error: { error: 'Strategic theme does not exist.' } }); // Change 20260726
    await render(); // Change 20260726
    component.form.patchValue({ titleEn: 'Doomed draft' }); // Change 20260726

    await component.saveDraft(); // Change 20260726
    fixture.detectChanges(); // Change 20260726

    expect(component.error()).toBe('Strategic theme does not exist.'); // Change 20260726
    expect(navigate).not.toHaveBeenCalled(); // Change 20260726
    const banner = (fixture.nativeElement as HTMLElement).querySelector( // Change 20260726
      '[data-testid="idea-new-error"]', // Change 20260726
    ); // Change 20260726
    expect(banner?.textContent).toContain('Strategic theme does not exist.'); // Change 20260726
    expect(component.saving()).toBeFalse(); // Change 20260726
  }); // Change 20260726

  it('tracks and removes picked attachments without uploading them immediately', async () => { // Change 20260726
    await render(); // Change 20260726
    const file = new File(['x'], 'plan.pdf', { type: 'application/pdf' }); // Change 20260726

    component.onFilesSelected({ target: { files: [file], value: 'plan.pdf' } } as unknown as Event); // Change 20260726

    expect(component.attachments()).toEqual([ // Change 20260726
      { filename: 'plan.pdf', sizeBytes: file.size, mimeType: 'application/pdf', url: null }, // Change 20260726
    ]); // Change 20260726
    expect(ideas.addAttachment).not.toHaveBeenCalled(); // Change 20260726

    component.removeAttachment(0); // Change 20260726
    expect(component.attachments()).toEqual([]); // Change 20260726
  }); // Change 20260726

  it('uploads picked attachments once the draft has an id', async () => { // Change 20260726
    ideas.addAttachment.and.resolveTo({ // Change 20260726
      id: 'a1', // Change 20260726
      fileName: 'plan.pdf', // Change 20260726
      contentType: 'application/pdf', // Change 20260726
      fileSizeBytes: 1, // Change 20260726
      uploadedAt: '2026-07-26T00:00:00Z', // Change 20260726
    }); // Change 20260726
    await render(); // Change 20260726
    const file = new File(['x'], 'plan.pdf', { type: 'application/pdf' }); // Change 20260726
    component.onFilesSelected({ target: { files: [file], value: '' } } as unknown as Event); // Change 20260726
    component.form.patchValue({ titleEn: 'With attachment' }); // Change 20260726

    await component.saveDraft(); // Change 20260726

    expect(ideas.addAttachment).toHaveBeenCalledWith('idea-1', file); // Change 20260726
  }); // Change 20260726
}); // Change 20260726

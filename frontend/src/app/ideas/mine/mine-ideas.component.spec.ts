import { ComponentFixture, TestBed } from '@angular/core/testing'; // Change 20260726
import { provideRouter } from '@angular/router'; // Change 20260726
import { HttpErrorResponse } from '@angular/common/http'; // Change 20260726
import { MineIdeasComponent } from './mine-ideas.component'; // Change 20260726
import { IdeasService, MineIdeaRow, MineIdeasPage } from '../ideas.service'; // Change 20260726

function row(overrides: Partial<MineIdeaRow> = {}): MineIdeaRow { // Change 20260726
  return { // Change 20260726
    id: 'i1', // Change 20260726
    code: 'IDEA-001', // Change 20260726
    titleAr: 'عنوان', // Change 20260726
    titleEn: 'Solar Rooftop Panels', // Change 20260726
    themeId: 't1', // Change 20260726
    themeNameAr: 'الطاقة', // Change 20260726
    themeNameEn: 'Energy', // Change 20260726
    status: 'submitted', // Change 20260726
    currentStage: 1, // Change 20260726
    createdAt: '2026-01-01T00:00:00Z', // Change 20260726
    updatedAt: '2026-01-05T00:00:00Z', // Change 20260726
    feedbackCount: 0, // Change 20260726
    isOwner: true, // Change 20260726
    ...overrides, // Change 20260726
  }; // Change 20260726
} // Change 20260726

function page(items: MineIdeaRow[], total = items.length): MineIdeasPage { // Change 20260726
  return { items, total, page: 1, size: 20 }; // Change 20260726
} // Change 20260726

describe('MineIdeasComponent', () => { // Change 20260726
  let fixture: ComponentFixture<MineIdeasComponent>; // Change 20260726
  let ideas: jasmine.SpyObj<IdeasService>; // Change 20260726

  async function setup(): Promise<void> { // Change 20260726
    ideas = jasmine.createSpyObj<IdeasService>('IdeasService', ['getMine', 'getById', 'withdrawIdea']); // Change 20260726
    TestBed.configureTestingModule({ // Change 20260726
      imports: [MineIdeasComponent], // Change 20260726
      providers: [provideRouter([]), { provide: IdeasService, useValue: ideas }], // Change 20260726
    }); // Change 20260726
  } // Change 20260726

  async function render(): Promise<void> { // Change 20260726
    fixture = TestBed.createComponent(MineIdeasComponent); // Change 20260726
    fixture.detectChanges(); // Change 20260726
    await fixture.whenStable(); // Change 20260726
    fixture.detectChanges(); // Change 20260726
  } // Change 20260726

  beforeEach(async () => await setup()); // Change 20260726

  it('renders rows once the service resolves', async () => { // Change 20260726
    ideas.getMine.and.resolveTo(page([row(), row({ id: 'i2', code: 'IDEA-002', titleEn: 'Water Recycling' })])); // Change 20260726
    await render(); // Change 20260726

    const text = (fixture.nativeElement as HTMLElement).textContent ?? ''; // Change 20260726
    expect(text).toContain('IDEA-001'); // Change 20260726
    expect(text).toContain('Solar Rooftop Panels'); // Change 20260726
    expect(text).toContain('Energy'); // Change 20260726
    expect(text).toContain('Water Recycling'); // Change 20260726
  }); // Change 20260726

  it('falls back to the Arabic title when the English one is missing', async () => { // Change 20260726
    ideas.getMine.and.resolveTo(page([row({ titleEn: null })])); // Change 20260726
    await render(); // Change 20260726

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('عنوان'); // Change 20260726
  }); // Change 20260726

  it('renders the empty state when no ideas are returned', async () => { // Change 20260726
    ideas.getMine.and.resolveTo(page([], 0)); // Change 20260726
    await render(); // Change 20260726

    const empty = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="mine-empty"]'); // Change 20260726
    expect(empty).toBeTruthy(); // Change 20260726
    expect(empty?.textContent).toContain("You haven't submitted any ideas yet"); // Change 20260726
  }); // Change 20260726

  it('renders the error banner when the request fails', async () => { // Change 20260726
    ideas.getMine.and.rejectWith(new HttpErrorResponse({ status: 500 })); // Change 20260726
    await render(); // Change 20260726

    const banner = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="mine-error"]'); // Change 20260726
    expect(banner?.textContent).toContain('Could not load ideas'); // Change 20260726
  }); // Change 20260726

  it('disables Edit for statuses the innovator cannot edit', async () => { // Change 20260726
    ideas.getMine.and.resolveTo(page([row({ status: 'submitted' })])); // Change 20260726
    await render(); // Change 20260726

    const editButton = (fixture.nativeElement as HTMLElement).querySelector('table button[disabled]'); // Change 20260726
    expect(editButton?.textContent?.trim()).toBe('Edit'); // Change 20260726
    expect(fixture.componentInstance.isEditable(row({ status: 'returned' }))).toBeTrue(); // Change 20260726
  }); // Change 20260726

  it('refetches when the status filter changes', async () => { // Change 20260726
    ideas.getMine.and.resolveTo(page([row()])); // Change 20260726
    await render(); // Change 20260726

    const select = (fixture.nativeElement as HTMLElement).querySelector('select') as HTMLSelectElement; // Change 20260726
    select.value = 'returned'; // Change 20260726
    select.dispatchEvent(new Event('change')); // Change 20260726
    await fixture.whenStable(); // Change 20260726

    expect(ideas.getMine).toHaveBeenCalledTimes(2); // Change 20260726
    expect(ideas.getMine.calls.mostRecent().args).toEqual([1, 20, 'returned', 'createdAt desc']); // Change 20260726
  }); // Change 20260726

  it('pages forward and clamps at the last page', async () => { // Change 20260726
    ideas.getMine.and.resolveTo(page([row()], 25)); // Change 20260726
    await render(); // Change 20260726

    expect(fixture.componentInstance.totalPages()).toBe(2); // Change 20260726
    fixture.componentInstance.goToPage(2); // Change 20260726
    await fixture.whenStable(); // Change 20260726
    expect(ideas.getMine.calls.mostRecent().args[0]).toBe(2); // Change 20260726

    fixture.componentInstance.goToPage(3); // Change 20260726
    expect(fixture.componentInstance.page()).toBe(2); // Change 20260726
  }); // Change 20260726

  it('sorts the fetched page alphabetically by status when Status sort is picked', async () => { // Change 20260726
    ideas.getMine.and.resolveTo(page([row({ status: 'submitted' }), row({ id: 'i2', status: 'approved' })])); // Change 20260726
    await render(); // Change 20260726

    fixture.componentInstance.onSortChange('status'); // Change 20260726
    await fixture.whenStable(); // Change 20260726

    expect(fixture.componentInstance.rows().map((r) => r.status)).toEqual(['approved', 'submitted']); // Change 20260726
  }); // Change 20260726

  it('opens the withdraw dialog for a withdrawable row and refreshes after success', async () => { // Change 20260726
    ideas.getMine.and.resolveTo(page([row({ status: 'submitted' })])); // Change 20260726
    ideas.withdrawIdea.and.resolveTo(undefined); // Change 20260726
    await render(); // Change 20260726
    const component = fixture.componentInstance; // Change 20260726

    component.openWithdraw(component.rows()[0]); // Change 20260726
    fixture.detectChanges(); // Change 20260726
    expect(component.withdrawTarget()).not.toBeNull(); // Change 20260726
    expect((fixture.nativeElement as HTMLElement).querySelector('app-withdraw-dialog')).toBeTruthy(); // Change 20260726

    const callsBefore = ideas.getMine.calls.count(); // Change 20260726
    component.onWithdrawn(); // Change 20260726
    await fixture.whenStable(); // Change 20260726

    expect(component.withdrawTarget()).toBeNull(); // Change 20260726
    expect(ideas.getMine.calls.count()).toBe(callsBefore + 1); // Change 20260726
  }); // Change 20260726

  it('closing the withdraw dialog leaves the list untouched', async () => { // Change 20260726
    ideas.getMine.and.resolveTo(page([row({ status: 'submitted' })])); // Change 20260726
    await render(); // Change 20260726
    const component = fixture.componentInstance; // Change 20260726

    component.openWithdraw(component.rows()[0]); // Change 20260726
    const callsBefore = ideas.getMine.calls.count(); // Change 20260726
    component.closeWithdraw(); // Change 20260726
    await fixture.whenStable(); // Change 20260726

    expect(component.withdrawTarget()).toBeNull(); // Change 20260726
    expect(ideas.getMine.calls.count()).toBe(callsBefore); // Change 20260726
    expect(ideas.withdrawIdea).not.toHaveBeenCalled(); // Change 20260726
  }); // Change 20260726

  it('only offers Withdraw for draft, submitted and returned ideas', async () => { // Change 20260726
    ideas.getMine.and.resolveTo(page([row()])); // Change 20260726
    await render(); // Change 20260726
    const component = fixture.componentInstance; // Change 20260726

    expect(component.isWithdrawable(row({ status: 'draft' }))).toBeTrue(); // Change 20260726
    expect(component.isWithdrawable(row({ status: 'submitted' }))).toBeTrue(); // Change 20260726
    expect(component.isWithdrawable(row({ status: 'returned' }))).toBeTrue(); // Change 20260726
    expect(component.isWithdrawable(row({ status: 'approved' }))).toBeFalse(); // Change 20260726
    expect(component.isWithdrawable(row({ status: 'withdrawn' }))).toBeFalse(); // Change 20260726
  }); // Change 20260726
}); // Change 20260726

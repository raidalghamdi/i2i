import { ComponentFixture, TestBed } from '@angular/core/testing'; // Change 20260726
import { WithdrawDialogComponent } from './withdraw-dialog.component'; // Change 20260726
import { IdeasService } from '../ideas.service'; // Change 20260726

describe('WithdrawDialogComponent', () => { // Change 20260726
  let fixture: ComponentFixture<WithdrawDialogComponent>; // Change 20260726
  let component: WithdrawDialogComponent; // Change 20260726
  let ideas: jasmine.SpyObj<IdeasService>; // Change 20260726

  beforeEach(async () => { // Change 20260726
    ideas = jasmine.createSpyObj<IdeasService>('IdeasService', ['withdrawIdea']); // Change 20260726
    TestBed.configureTestingModule({ // Change 20260726
      imports: [WithdrawDialogComponent], // Change 20260726
      providers: [{ provide: IdeasService, useValue: ideas }], // Change 20260726
    }); // Change 20260726
    fixture = TestBed.createComponent(WithdrawDialogComponent); // Change 20260726
    component = fixture.componentInstance; // Change 20260726
    component.ideaId = 'idea-1'; // Change 20260726
    fixture.detectChanges(); // Change 20260726
  }); // Change 20260726

  it('cancelling emits cancelled without calling the service', () => { // Change 20260726
    const cancelled = jasmine.createSpy('cancelled'); // Change 20260726
    component.cancelled.subscribe(cancelled); // Change 20260726

    component.cancel(); // Change 20260726

    expect(cancelled).toHaveBeenCalled(); // Change 20260726
    expect(ideas.withdrawIdea).not.toHaveBeenCalled(); // Change 20260726
  }); // Change 20260726

  it('confirming posts the reason and emits withdrawn', async () => { // Change 20260726
    ideas.withdrawIdea.and.resolveTo(undefined); // Change 20260726
    const withdrawn = jasmine.createSpy('withdrawn'); // Change 20260726
    component.withdrawn.subscribe(withdrawn); // Change 20260726
    component.reason.set('No longer relevant'); // Change 20260726

    await component.confirm(); // Change 20260726

    expect(ideas.withdrawIdea).toHaveBeenCalledWith('idea-1', 'No longer relevant'); // Change 20260726
    expect(withdrawn).toHaveBeenCalledWith('idea-1'); // Change 20260726
    expect(component.error()).toBeNull(); // Change 20260726
  }); // Change 20260726

  it('surfaces the backend message and stays open when withdrawal fails', async () => { // Change 20260726
    ideas.withdrawIdea.and.rejectWith({ error: { error: 'Idea is not withdrawable.' } }); // Change 20260726
    const withdrawn = jasmine.createSpy('withdrawn'); // Change 20260726
    component.withdrawn.subscribe(withdrawn); // Change 20260726

    await component.confirm(); // Change 20260726

    expect(component.error()).toBe('Idea is not withdrawable.'); // Change 20260726
    expect(withdrawn).not.toHaveBeenCalled(); // Change 20260726
    expect(component.saving()).toBeFalse(); // Change 20260726
  }); // Change 20260726

  it('renders the irreversibility warning', () => { // Change 20260726
    const text = (fixture.nativeElement as HTMLElement).textContent ?? ''; // Change 20260726
    expect(text).toContain('This action cannot be undone.'); // Change 20260726
  }); // Change 20260726
}); // Change 20260726

import { ComponentFixture, TestBed } from '@angular/core/testing'; // Change 20260726
import { WithdrawDialogComponent } from './withdraw-dialog.component'; // Change 20260726
import { IdeasApiService } from '../ideas-api.service'; // Change 20260726

describe('WithdrawDialogComponent', () => { // Change 20260726
  let fixture: ComponentFixture<WithdrawDialogComponent>; // Change 20260726
  let component: WithdrawDialogComponent; // Change 20260726
  let ideasApi: jasmine.SpyObj<IdeasApiService>; // Change 20260726

  beforeEach(() => { // Change 20260726
    ideasApi = jasmine.createSpyObj<IdeasApiService>('IdeasApiService', ['withdraw']); // Change 20260726
    TestBed.configureTestingModule({ // Change 20260726
      imports: [WithdrawDialogComponent], // Change 20260726
      providers: [{ provide: IdeasApiService, useValue: ideasApi }], // Change 20260726
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
    expect(ideasApi.withdraw).not.toHaveBeenCalled(); // Change 20260726
  }); // Change 20260726

  it('confirming posts the reason and emits withdrawn', async () => { // Change 20260726
    ideasApi.withdraw.and.resolveTo(undefined); // Change 20260726
    const withdrawn = jasmine.createSpy('withdrawn'); // Change 20260726
    component.withdrawn.subscribe(withdrawn); // Change 20260726
    component.reason.set('No longer relevant'); // Change 20260726

    await component.confirm(); // Change 20260726

    expect(ideasApi.withdraw).toHaveBeenCalledWith('idea-1', 'No longer relevant'); // Change 20260726
    expect(withdrawn).toHaveBeenCalledWith('idea-1'); // Change 20260726
    expect(component.error()).toBeNull(); // Change 20260726
  }); // Change 20260726

  it('omits an all-whitespace reason', async () => { // Change 20260726
    ideasApi.withdraw.and.resolveTo(undefined); // Change 20260726
    component.reason.set('   '); // Change 20260726

    await component.confirm(); // Change 20260726

    expect(ideasApi.withdraw).toHaveBeenCalledWith('idea-1', undefined); // Change 20260726
  }); // Change 20260726

  it('surfaces the backend message and stays open when withdrawal fails', async () => { // Change 20260726
    ideasApi.withdraw.and.rejectWith({ error: { error: 'Idea is not withdrawable.' } }); // Change 20260726
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

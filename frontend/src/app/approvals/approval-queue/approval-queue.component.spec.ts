import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ApprovalsApiService } from '../approvals-api.service';
import { PendingApproval } from '../approval.model';
import { ApprovalQueueComponent } from './approval-queue.component';

describe('ApprovalQueueComponent', () => {
  let fixture: ComponentFixture<ApprovalQueueComponent>;
  let approvalsApi: jasmine.SpyObj<ApprovalsApiService>;

  const cardOne: PendingApproval = {
    instanceId: 'i1',
    stepId: 's1',
    entityType: 'idea',
    entityId: 'idea-1',
    chainNameAr: 'سلسلة أ',
    chainNameEn: 'Chain A',
    stepLabelAr: 'خطوة ١',
    stepLabelEn: 'Step 1',
    stepOrder: 1,
    minApprovers: 2,
    priorApprovers: 1,
  };

  const cardTwo: PendingApproval = {
    instanceId: 'i2',
    stepId: 's2',
    entityType: 'idea',
    entityId: 'idea-2',
    chainNameAr: 'سلسلة ب',
    chainNameEn: 'Chain B',
    stepLabelAr: 'خطوة ٢',
    stepLabelEn: 'Step 2',
    stepOrder: 2,
    minApprovers: 3,
    priorApprovers: 0,
  };

  function setup(cards: PendingApproval[]): void {
    approvalsApi = jasmine.createSpyObj('ApprovalsApiService', ['list', 'decide', 'bulkDecide']);
    approvalsApi.list.and.returnValue(Promise.resolve(cards));
    approvalsApi.decide.and.returnValue(Promise.resolve());
    approvalsApi.bulkDecide.and.returnValue(Promise.resolve({ succeeded: 2, failed: [] }));

    TestBed.configureTestingModule({
      imports: [ApprovalQueueComponent],
      providers: [provideRouter([]), { provide: ApprovalsApiService, useValue: approvalsApi }],
    });
    fixture = TestBed.createComponent(ApprovalQueueComponent);
  }

  async function init(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('renders one card per pending approval with progress text', async () => {
    setup([cardOne, cardTwo]);
    await init();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Chain A');
    expect(text).toContain('Step 1');
    expect(text).toContain('1/2');
    expect(text).toContain('Chain B');
    expect(text).toContain('Step 2');
    expect(text).toContain('0/3');
  });

  it('shows an empty-state message when there are no pending approvals', async () => {
    setup([]);
    await init();

    expect(fixture.nativeElement.textContent).toContain('No approvals');
  });

  it('approves a card and reloads the list', async () => {
    setup([cardOne, cardTwo]);
    await init();

    approvalsApi.list.and.returnValue(Promise.resolve([cardTwo]));

    const approveButton = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b) => (b as HTMLButtonElement).textContent?.trim() === 'Approve',
    ) as HTMLButtonElement;
    approveButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(approvalsApi.decide).toHaveBeenCalledWith('i1', 's1', 'approve', undefined);
    expect(approvalsApi.list).toHaveBeenCalledTimes(2);
    expect(fixture.componentInstance.cards().length).toBe(1);
  });

  it('rejects a card and reloads the list', async () => {
    setup([cardOne, cardTwo]);
    await init();

    const rejectButton = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b) => (b as HTMLButtonElement).textContent?.trim() === 'Reject',
    ) as HTMLButtonElement;
    rejectButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(approvalsApi.decide).toHaveBeenCalledWith('i1', 's1', 'reject', undefined);
  });

  it('sets an error message when the decision fails', async () => {
    setup([cardOne, cardTwo]);
    await init();

    approvalsApi.decide.and.returnValue(Promise.reject(new Error('boom')));

    await fixture.componentInstance.onDecide(cardOne, 'approve');
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toContain('Could not record the decision');
    expect(fixture.nativeElement.textContent).toContain('Could not record the decision');
  });

  it('bulk-approves the selected cards when bulk mode is on', async () => {
    setup([cardOne, cardTwo]);
    await init();

    fixture.componentInstance.bulkMode.set(true);
    fixture.detectChanges();

    const checkboxes = Array.from(
      fixture.nativeElement.querySelectorAll('input[type="checkbox"]'),
    ) as HTMLInputElement[];
    expect(checkboxes.length).toBe(2);
    checkboxes[0].click();
    checkboxes[1].click();
    fixture.detectChanges();

    await fixture.componentInstance.onBulkDecide('approve');

    expect(approvalsApi.bulkDecide).toHaveBeenCalledWith(
      jasmine.arrayContaining([
        { instanceId: 'i1', stepId: 's1' },
        { instanceId: 'i2', stepId: 's2' },
      ]),
      'approve',
      undefined,
    );
    expect(fixture.componentInstance.selected().size).toBe(0);
  });

  it('sets an error message when the bulk decision fails', async () => {
    setup([cardOne, cardTwo]);
    await init();

    fixture.componentInstance.bulkMode.set(true);
    fixture.componentInstance.selected.set(new Set(['i1']));
    approvalsApi.bulkDecide.and.returnValue(Promise.reject(new Error('boom')));

    await fixture.componentInstance.onBulkDecide('reject');
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBeTruthy();
  });

  it('shows the error state and retries the fetch when "Try again" is clicked', async () => {
    approvalsApi = jasmine.createSpyObj('ApprovalsApiService', ['list', 'decide', 'bulkDecide']);
    approvalsApi.list.and.returnValue(Promise.reject(new Error('boom')));

    TestBed.configureTestingModule({
      imports: [ApprovalQueueComponent],
      providers: [provideRouter([]), { provide: ApprovalsApiService, useValue: approvalsApi }],
    });
    fixture = TestBed.createComponent(ApprovalQueueComponent);
    await init();

    expect(fixture.componentInstance.loadError()).not.toBeNull();
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();

    approvalsApi.list.and.returnValue(Promise.resolve([cardOne]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.componentInstance.cards().length).toBe(1);
  });
});

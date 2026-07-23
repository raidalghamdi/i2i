import { Component, Inject, LOCALE_ID, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { ApprovalDecision, ApprovalsApiService } from '../approvals-api.service';
import { PendingApproval } from '../approval.model';

@Component({
  selector: 'app-approval-queue',
  imports: [FormsModule, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './approval-queue.component.html',
})
export class ApprovalQueueComponent implements OnInit {
  private readonly approvalsApi = inject(ApprovalsApiService);
  private readonly isArabic: boolean;

  readonly cards = signal<PendingApproval[]>([]);
  readonly loading = signal(false);
  readonly comment = signal('');
  readonly bulkMode = signal(false);
  readonly selected = signal<Set<string>>(new Set());
  readonly errorMessage = signal<string | null>(null);
  readonly loadError = signal<string | null>(null);

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      this.cards.set(await this.approvalsApi.list());
    } catch {
      this.loadError.set($localize`:@@approvalsLoadError:Couldn't load approvals. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }

  chainName(card: PendingApproval): string {
    return this.isArabic ? card.chainNameAr : card.chainNameEn;
  }

  stepLabel(card: PendingApproval): string {
    return this.isArabic ? card.stepLabelAr : card.stepLabelEn;
  }

  toggleBulkMode(): void {
    this.bulkMode.update((v) => !v);
    this.selected.set(new Set());
  }

  isSelected(card: PendingApproval): boolean {
    return this.selected().has(card.instanceId);
  }

  toggleSelection(card: PendingApproval): void {
    const next = new Set(this.selected());
    if (next.has(card.instanceId)) {
      next.delete(card.instanceId);
    } else {
      next.add(card.instanceId);
    }
    this.selected.set(next);
  }

  async onDecide(card: PendingApproval, decision: ApprovalDecision): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.approvalsApi.decide(card.instanceId, card.stepId, decision, this.comment() || undefined);
      await this.reload();
    } catch {
      this.errorMessage.set(
        $localize`:@@approvalsDecideError:Could not record the decision. Please try again.`,
      );
    }
  }

  async onBulkDecide(decision: ApprovalDecision): Promise<void> {
    this.errorMessage.set(null);
    const targets = this.cards()
      .filter((c) => this.selected().has(c.instanceId))
      .map((c) => ({ instanceId: c.instanceId, stepId: c.stepId }));

    try {
      const result = await this.approvalsApi.bulkDecide(targets, decision, this.comment() || undefined);
      if (result.failed.length > 0) {
        this.errorMessage.set(
          $localize`:@@approvalsBulkPartialError:Some decisions could not be recorded. Please try again.`,
        );
      }
      this.selected.set(new Set());
      await this.reload();
    } catch {
      this.errorMessage.set(
        $localize`:@@approvalsBulkError:Could not record the bulk decision. Please try again.`,
      );
    }
  }
}

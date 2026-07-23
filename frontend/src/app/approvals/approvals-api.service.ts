import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApprovalBulkResult, PendingApproval } from './approval.model';

export type ApprovalDecision = 'approve' | 'reject';

interface ApprovalBulkTarget {
  instanceId: string;
  stepId: string;
}

@Injectable({ providedIn: 'root' })
export class ApprovalsApiService {
  private readonly http = inject(HttpClient);

  async list(): Promise<PendingApproval[]> {
    const response = await firstValueFrom(
      this.http.get<{ items: PendingApproval[] }>('/api/approvals'),
    );
    return response.items;
  }

  decide(
    instanceId: string,
    stepId: string,
    decision: ApprovalDecision,
    comment?: string,
  ): Promise<void> {
    return firstValueFrom(
      this.http.post<void>('/api/approvals/decide', { instanceId, stepId, decision, comment }),
    );
  }

  bulkDecide(
    targets: ApprovalBulkTarget[],
    decision: ApprovalDecision,
    comment?: string,
  ): Promise<ApprovalBulkResult> {
    return firstValueFrom(
      this.http.post<ApprovalBulkResult>('/api/approvals/bulk-decide', {
        targets,
        decision,
        comment,
      }),
    );
  }
}

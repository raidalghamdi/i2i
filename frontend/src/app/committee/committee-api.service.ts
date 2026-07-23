import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CommitteeCriterion, CommitteeDecisionInput, CommitteeDecisionResult, CommitteeQueueItem, MyCommitteeDecision } from './committee.model';

@Injectable({ providedIn: 'root' })
export class CommitteeApiService {
  private readonly http = inject(HttpClient);

  submitToCommittee(ideaId: string): Promise<{ id: string; status: string }> {
    return firstValueFrom(this.http.post<{ id: string; status: string }>(`/api/ideas/${ideaId}/submit-to-committee`, null));
  }

  getCriteria(): Promise<CommitteeCriterion[]> {
    return firstValueFrom(this.http.get<CommitteeCriterion[]>('/api/committee-criteria'));
  }

  submitDecision(ideaId: string, input: CommitteeDecisionInput): Promise<CommitteeDecisionResult> {
    return firstValueFrom(this.http.post<CommitteeDecisionResult>(`/api/ideas/${ideaId}/committee-decisions`, input));
  }

  getQueue(): Promise<CommitteeQueueItem[]> {
    return firstValueFrom(this.http.get<CommitteeQueueItem[]>('/api/committee/queue'));
  }

  getMine(): Promise<MyCommitteeDecision[]> {
    return firstValueFrom(this.http.get<MyCommitteeDecision[]>('/api/committee/mine'));
  }
}

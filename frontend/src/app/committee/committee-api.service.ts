import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CommitteeCriterion, CommitteeDecisionInput, CommitteeDecisionResult, CommitteeQueueItem, MyCommitteeDecision } from './committee.model';

@Injectable({ providedIn: 'root' })
export class CommitteeApiService {
  private readonly http = inject(HttpClient);

  submitToCommittee(ideaId: string, judgeIds: string[]): Promise<{ id: string; status: string }> {
    return firstValueFrom(
      this.http.post<{ id: string; status: string }>(`/api/ideas/${ideaId}/submit-to-committee`, { judgeIds }),
    );
  }

  getCriteria(): Promise<CommitteeCriterion[]> {
    return firstValueFrom(this.http.get<CommitteeCriterion[]>('/api/committee-criteria'));
  }

  // Change 20260726 — the endpoint is multipart-only now, so the scores ride along as a JSON part.
  submitDecision(ideaId: string, input: CommitteeDecisionInput, files: File[] = []): Promise<CommitteeDecisionResult> {
    const form = new FormData();
    form.append('decisionType', input.decisionTypeCode);
    form.append('criteriaScores', JSON.stringify(input.criteriaScores));
    if (input.comments) form.append('comments', input.comments);
    for (const file of files) form.append('attachments', file, file.name);
    return firstValueFrom(this.http.post<CommitteeDecisionResult>(`/api/ideas/${ideaId}/committee-decisions`, form));
  }

  // Change 20260726 — fetched through HttpClient rather than linked directly, so the auth
  // interceptors can attach credentials that a plain anchor href would omit.
  getAttachment(decisionId: string, attachmentId: string): Promise<Blob> {
    return firstValueFrom(
      this.http.get(`/api/committee-decisions/${decisionId}/attachments/${attachmentId}`, { responseType: 'blob' }),
    );
  }

  getQueue(): Promise<CommitteeQueueItem[]> {
    return firstValueFrom(this.http.get<CommitteeQueueItem[]>('/api/committee/queue'));
  }

  getMine(): Promise<MyCommitteeDecision[]> {
    return firstValueFrom(this.http.get<MyCommitteeDecision[]>('/api/committee/mine'));
  }
}

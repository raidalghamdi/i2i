import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { EvaluationReviewDecisionInput, EvaluationReviewDetail } from './evaluation-review.model';
import { SupervisorQueueItem } from '../supervisor/supervisor.model';

@Injectable({ providedIn: 'root' })
export class EvaluationReviewApiService {
  private readonly http = inject(HttpClient);

  getDetail(ideaId: string): Promise<EvaluationReviewDetail> {
    return firstValueFrom(this.http.get<EvaluationReviewDetail>(`/api/ideas/${ideaId}/evaluation-detail`));
  }

  submitDecision(ideaId: string, input: EvaluationReviewDecisionInput): Promise<{ id: string; status: string }> {
    return firstValueFrom(
      this.http.post<{ id: string; status: string }>(`/api/ideas/${ideaId}/evaluation-review-decision`, input),
    );
  }

  getReviewQueue(): Promise<SupervisorQueueItem[]> {
    return firstValueFrom(this.http.get<SupervisorQueueItem[]>('/api/supervisor/evaluation-review-queue'));
  }

  getCommitteePendingQueue(): Promise<SupervisorQueueItem[]> {
    return firstValueFrom(this.http.get<SupervisorQueueItem[]>('/api/supervisor/committee-pending-queue'));
  }
}

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { EvaluationCriterion, EvaluationInput, EvaluationQueueItem, EvaluationSubmitResult, MyEvaluation } from './evaluation.model'; // Change 20260726

@Injectable({ providedIn: 'root' })
export class EvaluationsApiService {
  private readonly http = inject(HttpClient);

  submit(ideaId: string, input: EvaluationInput): Promise<EvaluationSubmitResult> {
    return firstValueFrom(this.http.post<EvaluationSubmitResult>(`/api/ideas/${ideaId}/evaluations`, input));
  }

  // Change 20260726 — the scoring criteria are admin-configurable, so the form builds itself from this.
  getCriteria(): Promise<EvaluationCriterion[]> {
    return firstValueFrom(this.http.get<EvaluationCriterion[]>('/api/evaluation-criteria'));
  }

  getQueue(): Promise<EvaluationQueueItem[]> {
    return firstValueFrom(this.http.get<EvaluationQueueItem[]>('/api/evaluations/queue'));
  }

  getMine(): Promise<MyEvaluation[]> {
    return firstValueFrom(this.http.get<MyEvaluation[]>('/api/evaluations/mine'));
  }
}

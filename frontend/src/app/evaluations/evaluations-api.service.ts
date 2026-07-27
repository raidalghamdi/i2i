import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { EvaluationInput, EvaluationQueueItem, EvaluationSubmitResult, MyEvaluation } from './evaluation.model';

@Injectable({ providedIn: 'root' })
export class EvaluationsApiService {
  private readonly http = inject(HttpClient);

  submit(ideaId: string, input: EvaluationInput): Promise<EvaluationSubmitResult> {
    return firstValueFrom(this.http.post<EvaluationSubmitResult>(`/api/ideas/${ideaId}/evaluations`, input));
  }

  getQueue(): Promise<EvaluationQueueItem[]> {
    return firstValueFrom(this.http.get<EvaluationQueueItem[]>('/api/evaluations/queue'));
  }

  getMine(): Promise<MyEvaluation[]> {
    return firstValueFrom(this.http.get<MyEvaluation[]>('/api/evaluations/mine'));
  }
}

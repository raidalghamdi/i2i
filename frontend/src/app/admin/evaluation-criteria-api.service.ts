import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { EvaluationCriterion, EvaluationCriterionInput } from './evaluation-criteria.model';

// Change 20260726
@Injectable({ providedIn: 'root' })
export class EvaluationCriteriaApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<EvaluationCriterion[]> {
    return firstValueFrom(this.http.get<EvaluationCriterion[]>('/api/admin/evaluation-criteria'));
  }

  create(input: EvaluationCriterionInput): Promise<EvaluationCriterion> {
    return firstValueFrom(
      this.http.post<EvaluationCriterion>('/api/admin/evaluation-criteria', input),
    );
  }

  update(id: string, input: EvaluationCriterionInput): Promise<EvaluationCriterion> {
    return firstValueFrom(
      this.http.put<EvaluationCriterion>(`/api/admin/evaluation-criteria/${id}`, input),
    );
  }

  remove(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/admin/evaluation-criteria/${id}`));
  }
}

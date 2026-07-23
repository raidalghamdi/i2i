import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CommitteeCriterion, CommitteeCriterionInput } from './committee-criteria.model';

@Injectable({ providedIn: 'root' })
export class CommitteeCriteriaApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<CommitteeCriterion[]> {
    return firstValueFrom(this.http.get<CommitteeCriterion[]>('/api/admin/committee-criteria'));
  }

  create(input: CommitteeCriterionInput): Promise<CommitteeCriterion> {
    return firstValueFrom(this.http.post<CommitteeCriterion>('/api/admin/committee-criteria', input));
  }

  update(id: string, input: CommitteeCriterionInput): Promise<CommitteeCriterion> {
    return firstValueFrom(
      this.http.put<CommitteeCriterion>(`/api/admin/committee-criteria/${id}`, input),
    );
  }

  remove(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/admin/committee-criteria/${id}`));
  }
}

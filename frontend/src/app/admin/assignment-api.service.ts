import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  Assignment,
  AssignmentCreateInput,
  AssignmentListFilter,
  AssignmentPage,
  AssignmentUpdateInput,
  AssignmentUpdateResult,
  BulkAssignmentCreateResult,
  IdeaOption,
  SuggestedEvaluator,
  WorkloadRow,
} from './assignment.model';

@Injectable({ providedIn: 'root' })
export class AssignmentApiService {
  private readonly http = inject(HttpClient);

  list(filter: AssignmentListFilter): Promise<AssignmentPage> {
    let params = new HttpParams().set('page', filter.page).set('pageSize', filter.pageSize);
    if (filter.evaluatorId) params = params.set('evaluatorId', filter.evaluatorId);
    if (filter.status) params = params.set('status', filter.status);
    if (filter.ideaSearch) params = params.set('ideaSearch', filter.ideaSearch);
    return firstValueFrom(this.http.get<AssignmentPage>('/api/admin/assignments', { params }));
  }

  getWorkloadHeatmap(): Promise<WorkloadRow[]> {
    return firstValueFrom(this.http.get<WorkloadRow[]>('/api/admin/assignments/workload-heatmap'));
  }

  suggestEvaluators(): Promise<SuggestedEvaluator[]> {
    return firstValueFrom(this.http.get<SuggestedEvaluator[]>('/api/admin/assignments/suggest-evaluators'));
  }

  listIdeaOptions(): Promise<IdeaOption[]> {
    return firstValueFrom(this.http.get<IdeaOption[]>('/api/admin/assignments/idea-options'));
  }

  create(input: AssignmentCreateInput): Promise<{ id: string }> {
    return firstValueFrom(this.http.post<{ id: string }>('/api/admin/assignments', input));
  }

  bulkCreate(inputs: AssignmentCreateInput[]): Promise<BulkAssignmentCreateResult> {
    return firstValueFrom(this.http.post<BulkAssignmentCreateResult>('/api/admin/assignments/bulk', { assignments: inputs }));
  }

  update(id: string, input: AssignmentUpdateInput): Promise<AssignmentUpdateResult> {
    return firstValueFrom(this.http.patch<AssignmentUpdateResult>(`/api/admin/assignments/${id}`, input));
  }

  unassign(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/admin/assignments/${id}`));
  }

  bulkUnassign(ids: string[]): Promise<{ unassigned: number }> {
    return firstValueFrom(this.http.post<{ unassigned: number }>('/api/admin/assignments/bulk-unassign', { ids }));
  }
}

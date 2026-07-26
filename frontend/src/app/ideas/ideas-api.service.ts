import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  Idea,
  IdeaAttachment,
  IdeaInput,
  IdeaResubmitInput,
  IdeaJourney,
  IdeaListFilters,
  IdeaListPage,
  IdeaSummary,
  MyIdeaItem,
} from './idea.model';

export interface EvaluationSummaryItem {
  reviewerLabel: string;
  comment: string | null;
}

export interface EvaluationSummary {
  evaluations: EvaluationSummaryItem[];
  supervisorComment: string | null;
}

@Injectable({ providedIn: 'root' })
export class IdeasApiService {
  private readonly http = inject(HttpClient);

  create(input: IdeaInput): Promise<{ id: string; code: string; status: string }> {
    return firstValueFrom(this.http.post<{ id: string; code: string; status: string }>('/api/ideas', input));
  }

  update(id: string, input: IdeaInput): Promise<{ id: string; code: string }> {
    return firstValueFrom(this.http.put<{ id: string; code: string }>(`/api/ideas/${id}`, input));
  }

  submit(id: string): Promise<{ id: string; status: string }> {
    return firstValueFrom(this.http.post<{ id: string; status: string }>(`/api/ideas/${id}/submit`, null));
  }

  /** Resubmit a returned idea. The backend enforces which sections may change via editableSections. */
  resubmit(id: string, input: IdeaResubmitInput): Promise<{ id: string; status: string }> {
    return firstValueFrom(this.http.post<{ id: string; status: string }>(`/api/ideas/${id}/resubmit`, input));
  }

  getMine(): Promise<IdeaSummary[]> {
    return firstValueFrom(this.http.get<IdeaSummary[]>('/api/ideas/mine'));
  }

  getById(id: string): Promise<Idea> {
    return firstValueFrom(this.http.get<Idea>(`/api/ideas/${id}`));
  }

  uploadAttachment(id: string, file: File): Promise<IdeaAttachment> {
    const formData = new FormData();
    formData.append('file', file);
    return firstValueFrom(this.http.post<IdeaAttachment>(`/api/ideas/${id}/attachments`, formData));
  }

  getAttachments(id: string): Promise<IdeaAttachment[]> {
    return firstValueFrom(this.http.get<IdeaAttachment[]>(`/api/ideas/${id}/attachments`));
  }

  getAttachmentBlob(ideaId: string, attachmentId: string): Promise<Blob> {
    return firstValueFrom(this.http.get(`/api/ideas/${ideaId}/attachments/${attachmentId}`, { responseType: 'blob' }));
  }

  getEvaluations(id: string): Promise<EvaluationSummary> {
    return firstValueFrom(this.http.get<EvaluationSummary>(`/api/ideas/${id}/evaluations`));
  }

  resubmitEvaluation(id: string, comment: string): Promise<{ id: string; status: string }> {
    return firstValueFrom(
      this.http.post<{ id: string; status: string }>(`/api/ideas/${id}/resubmit-evaluation`, { comment }),
    );
  }

  getJourney(id: string): Promise<IdeaJourney> {
    return firstValueFrom(this.http.get<IdeaJourney>(`/api/ideas/${id}/journey`));
  }

  list(filters: IdeaListFilters): Promise<IdeaListPage> {
    let params = new HttpParams();
    if (filters.q) params = params.set('q', filters.q);
    if (filters.strategicThemeId) params = params.set('strategicThemeId', filters.strategicThemeId);
    if (filters.activityId) params = params.set('activityId', filters.activityId);
    if (filters.status) params = params.set('status', filters.status);
    if (filters.stage !== undefined) params = params.set('stage', filters.stage);
    if (filters.page !== undefined) params = params.set('page', filters.page);
    if (filters.pageSize !== undefined) params = params.set('pageSize', filters.pageSize);
    return firstValueFrom(this.http.get<IdeaListPage>('/api/ideas', { params }));
  }

  getMineDetailed(statusGroup?: string): Promise<MyIdeaItem[]> {
    let params = new HttpParams();
    if (statusGroup) params = params.set('statusGroup', statusGroup);
    return firstValueFrom(this.http.get<MyIdeaItem[]>('/api/ideas/mine', { params }));
  }

  withdraw(id: string, reason?: string): Promise<void> { // Change 20260726
    return firstValueFrom(this.http.post<void>(`/api/ideas/${id}/withdraw`, { reason: reason ?? null })); // Change 20260726
  } // Change 20260726

  deleteAttachment(ideaId: string, attachmentId: string): Promise<void> { // Change 20260726
    return firstValueFrom(this.http.delete<void>(`/api/ideas/${ideaId}/attachments/${attachmentId}`)); // Change 20260726
  } // Change 20260726
}

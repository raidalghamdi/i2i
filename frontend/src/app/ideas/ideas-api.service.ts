import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  Idea,
  IdeaAttachment,
  IdeaInput,
  IdeaJourney,
  IdeaListFilters,
  IdeaListPage,
  IdeaSummary,
  MyIdeaItem,
} from './idea.model';

export interface EvaluationSummaryItem {
  reviewerLabel: string;
  score: number | null;
  comment: string | null;
}

export interface EvaluationSummary {
  evaluations: EvaluationSummaryItem[];
  averageScore: number | null;
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

  getEvaluations(id: string): Promise<EvaluationSummary> {
    return firstValueFrom(this.http.get<EvaluationSummary>(`/api/ideas/${id}/evaluations`));
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

  withdraw(id: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`/api/ideas/${id}/withdraw`, null));
  }
}

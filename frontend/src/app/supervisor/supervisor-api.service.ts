import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  FinalRankingResult,
  RoleUser,
  ScreeningDecisionInput,
  ScreeningDecisionResult,
  ScreeningQueueItem,
  TrackAssignment,
  TrackAssignmentInput,
} from './supervisor.model';

@Injectable({ providedIn: 'root' })
export class SupervisorApiService {
  private readonly http = inject(HttpClient);

  getScreeningQueue(): Promise<ScreeningQueueItem[]> {
    return firstValueFrom(this.http.get<ScreeningQueueItem[]>('/api/screening/queue'));
  }

  submitScreeningDecision(ideaId: string, input: ScreeningDecisionInput): Promise<ScreeningDecisionResult> {
    return firstValueFrom(this.http.post<ScreeningDecisionResult>(`/api/ideas/${ideaId}/screening-decision`, input));
  }

  getTrackAssignments(): Promise<TrackAssignment[]> {
    return firstValueFrom(this.http.get<TrackAssignment[]>('/api/track-assignments'));
  }

  createTrackAssignment(input: TrackAssignmentInput): Promise<{ id: string }> {
    return firstValueFrom(this.http.post<{ id: string }>('/api/track-assignments', input));
  }

  removeTrackAssignment(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/track-assignments/${id}`));
  }

  getUsersByRole(role: string): Promise<RoleUser[]> {
    return firstValueFrom(this.http.get<RoleUser[]>(`/api/users?role=${role}`));
  }

  previewFinalRanking(): Promise<FinalRankingResult> {
    return firstValueFrom(this.http.get<FinalRankingResult>('/api/final-ranking/preview'));
  }

  runFinalRanking(): Promise<FinalRankingResult> {
    return firstValueFrom(this.http.post<FinalRankingResult>('/api/final-ranking/run', null));
  }
}

import { HttpClient, HttpParams } from '@angular/common/http'; // Change 20260726
import { Injectable, inject } from '@angular/core'; // Change 20260726
import { firstValueFrom } from 'rxjs'; // Change 20260726
import { Idea } from './idea.model'; // Change 20260726

/** A row of GET /api/ideas/mine (paginated envelope). */ // Change 20260726
export interface MineIdeaRow { // Change 20260726
  id: string; // Change 20260726
  code: string; // Change 20260726
  titleAr: string | null; // Change 20260726
  titleEn: string | null; // Change 20260726
  themeId: string | null; // Change 20260726
  themeNameAr: string | null; // Change 20260726
  themeNameEn: string | null; // Change 20260726
  status: string; // Change 20260726
  currentStage: number; // Change 20260726
  createdAt: string; // Change 20260726
  updatedAt: string; // Change 20260726
  feedbackCount: number; // Change 20260726
  isOwner: boolean; // Change 20260726
} // Change 20260726

export interface MineIdeasPage { // Change 20260726
  items: MineIdeaRow[]; // Change 20260726
  total: number; // Change 20260726
  page: number; // Change 20260726
  size: number; // Change 20260726
} // Change 20260726

/** One hash-chained audit row; `payload` is the raw JSON string written by the backend. */ // Change 20260726
export interface IdeaAuditEntry { // Change 20260726
  action: string; // Change 20260726
  actorId: string | null; // Change 20260726
  occurredAt: string; // Change 20260726
  payload: string | null; // Change 20260726
} // Change 20260726

export interface IdeaDetailView extends Idea { // Change 20260726
  auditTrail: IdeaAuditEntry[]; // Change 20260726
} // Change 20260726

/** Sort keys accepted by GET /api/ideas/mine. */ // Change 20260726
export type MineIdeasSort = 'createdAt desc' | 'createdAt asc' | 'updatedAt desc'; // Change 20260726

@Injectable({ providedIn: 'root' }) // Change 20260726
export class IdeasService { // Change 20260726
  private readonly http = inject(HttpClient); // Change 20260726

  getMine(page: number, size: number, status?: string, sort?: string): Promise<MineIdeasPage> { // Change 20260726
    let params = new HttpParams().set('page', page).set('size', size); // Change 20260726
    if (status) params = params.set('status', status); // Change 20260726
    if (sort) params = params.set('sort', sort); // Change 20260726
    return firstValueFrom(this.http.get<MineIdeasPage>('/api/ideas/mine', { params })); // Change 20260726
  } // Change 20260726

  getById(id: string): Promise<IdeaDetailView> { // Change 20260726
    return firstValueFrom(this.http.get<IdeaDetailView>(`/api/ideas/${id}`)); // Change 20260726
  } // Change 20260726
} // Change 20260726

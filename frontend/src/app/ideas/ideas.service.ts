import { HttpClient, HttpParams } from '@angular/common/http'; // Change 20260726
import { Injectable, inject } from '@angular/core'; // Change 20260726
import { firstValueFrom } from 'rxjs'; // Change 20260726
import { Idea, IdeaAttachment, StrategicTheme } from './idea.model'; // Change 20260726

/** // Change 20260726
 * The subset of idea fields the Phase 2c innovator forms collect. // Change 20260726
 * // Change 20260726
 * POST/PUT /api/ideas bind to the domain's `IdeaInput`, which has more required // Change 20260726
 * members than these screens ask for (activity, participation, consent, and the // Change 20260726
 * solution/benefits prose). `toIdeaInput` widens a draft into that contract so the // Change 20260726
 * request body stays shape-valid; CHANGES_20260726.md lists what is still unset. // Change 20260726
 */ // Change 20260726
export interface IdeaDraftInput { // Change 20260726
  titleEn: string; // Change 20260726
  titleAr: string; // Change 20260726
  descriptionEn: string; // Change 20260726
  descriptionAr: string; // Change 20260726
  strategicThemeId: string; // Change 20260726
} // Change 20260726

/** Attachment metadata held client-side before there is an idea to attach it to. */ // Change 20260726
export interface PendingAttachment { // Change 20260726
  filename: string; // Change 20260726
  sizeBytes: number; // Change 20260726
  mimeType: string; // Change 20260726
  url: string | null; // Change 20260726
} // Change 20260726

/** Widens a Phase 2c draft onto the backend's `IdeaInput` body. */ // Change 20260726
export function toIdeaInput(draft: IdeaDraftInput): Record<string, unknown> { // Change 20260726
  return { // Change 20260726
    titleAr: draft.titleAr, // Change 20260726
    titleEn: draft.titleEn, // Change 20260726
    // The form carries one description per language; it maps to the problem // Change 20260726
    // statement, the first of the backend's three prose fields. // Change 20260726
    problemStatementAr: draft.descriptionAr, // Change 20260726
    problemStatementEn: draft.descriptionEn, // Change 20260726
    proposedSolutionAr: '', // Change 20260726
    proposedSolutionEn: '', // Change 20260726
    expectedBenefitsAr: '', // Change 20260726
    expectedBenefitsEn: '', // Change 20260726
    strategicThemeId: draft.strategicThemeId, // Change 20260726
    challengeId: null, // Change 20260726
    participationType: 'individual', // Change 20260726
    teamName: null, // Change 20260726
    teamMembers: [], // Change 20260726
  }; // Change 20260726
} // Change 20260726

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

  getStrategicThemes(): Promise<StrategicTheme[]> { // Change 20260726
    return firstValueFrom(this.http.get<StrategicTheme[]>('/api/strategic-themes')); // Change 20260726
  } // Change 20260726

  createDraft(draft: IdeaDraftInput): Promise<{ id: string; code: string; status: string }> { // Change 20260726
    return firstValueFrom( // Change 20260726
      this.http.post<{ id: string; code: string; status: string }>('/api/ideas', toIdeaInput(draft)), // Change 20260726
    ); // Change 20260726
  } // Change 20260726

  updateIdea(id: string, draft: IdeaDraftInput): Promise<{ id: string; code: string }> { // Change 20260726
    return firstValueFrom( // Change 20260726
      this.http.put<{ id: string; code: string }>(`/api/ideas/${id}`, toIdeaInput(draft)), // Change 20260726
    ); // Change 20260726
  } // Change 20260726

  submitIdea(id: string): Promise<{ id: string; status: string }> { // Change 20260726
    return firstValueFrom( // Change 20260726
      this.http.post<{ id: string; status: string }>(`/api/ideas/${id}/submit`, null), // Change 20260726
    ); // Change 20260726
  } // Change 20260726

  withdrawIdea(id: string, reason?: string): Promise<void> { // Change 20260726
    return firstValueFrom( // Change 20260726
      this.http.post<void>(`/api/ideas/${id}/withdraw`, { reason: reason?.trim() || null }), // Change 20260726
    ); // Change 20260726
  } // Change 20260726

  deleteAttachment(ideaId: string, attachmentId: string): Promise<void> { // Change 20260726
    return firstValueFrom( // Change 20260726
      this.http.delete<void>(`/api/ideas/${ideaId}/attachments/${attachmentId}`), // Change 20260726
    ); // Change 20260726
  } // Change 20260726

  /** // Change 20260726
   * POST /api/ideas/:id/attachments binds an `IFormFile`, so this sends multipart // Change 20260726
   * form data rather than the JSON metadata the Phase 2c brief described. // Change 20260726
   */ // Change 20260726
  addAttachment(ideaId: string, file: File): Promise<IdeaAttachment> { // Change 20260726
    const body = new FormData(); // Change 20260726
    body.append('file', file, file.name); // Change 20260726
    return firstValueFrom( // Change 20260726
      this.http.post<IdeaAttachment>(`/api/ideas/${ideaId}/attachments`, body), // Change 20260726
    ); // Change 20260726
  } // Change 20260726
} // Change 20260726

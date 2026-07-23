import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { PostProgramIdea } from './post-program.model';

@Injectable({ providedIn: 'root' })
export class PostProgramApiService {
  private readonly http = inject(HttpClient);

  getIdeas(): Promise<PostProgramIdea[]> {
    return firstValueFrom(this.http.get<PostProgramIdea[]>('/api/admin/post-program/ideas'));
  }

  advance(ideaId: string, stage: string): Promise<{ id: string; status: string }> {
    return firstValueFrom(this.http.post<{ id: string; status: string }>(`/api/admin/ideas/${ideaId}/post-program-stage`, { stage }));
  }
}

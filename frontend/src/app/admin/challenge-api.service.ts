import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Challenge, ChallengeInput } from './challenge.model';

@Injectable({ providedIn: 'root' })
export class ChallengeApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<Challenge[]> {
    return firstValueFrom(this.http.get<Challenge[]>('/api/admin/challenges'));
  }

  getById(id: string): Promise<Challenge> {
    return firstValueFrom(this.http.get<Challenge>(`/api/admin/challenges/${id}`));
  }

  create(input: ChallengeInput): Promise<Challenge> {
    return firstValueFrom(this.http.post<Challenge>('/api/admin/challenges', input));
  }

  update(id: string, input: ChallengeInput): Promise<Challenge> {
    return firstValueFrom(this.http.put<Challenge>(`/api/admin/challenges/${id}`, input));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/admin/challenges/${id}`));
  }
}

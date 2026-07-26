import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Challenge } from './idea.model';

@Injectable({ providedIn: 'root' })
export class ChallengesService {
  private readonly http = inject(HttpClient);

  listByTheme(themeId: string): Promise<Challenge[]> {
    return firstValueFrom(this.http.get<Challenge[]>(`/api/challenges?themeId=${themeId}`));
  }
}

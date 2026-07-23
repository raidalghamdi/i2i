import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { StrategicTheme } from './idea.model';

export interface StrategicThemeInput {
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
}

@Injectable({ providedIn: 'root' })
export class StrategicThemesService {
  private readonly http = inject(HttpClient);

  list(): Promise<StrategicTheme[]> {
    return firstValueFrom(this.http.get<StrategicTheme[]>('/api/strategic-themes'));
  }

  create(input: StrategicThemeInput): Promise<StrategicTheme> {
    return firstValueFrom(this.http.post<StrategicTheme>('/api/strategic-themes', input));
  }

  update(id: string, input: StrategicThemeInput): Promise<StrategicTheme> {
    return firstValueFrom(this.http.patch<StrategicTheme>(`/api/strategic-themes/${id}`, input));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/strategic-themes/${id}`));
  }
}

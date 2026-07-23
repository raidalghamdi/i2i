import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface MeProfile {
  id: string;
  samAccountName: string;
  email: string;
  fullNameAr: string;
  fullNameEn: string;
  department: string | null;
  title: string | null;
  points: number;
  level: number;
  roles: string[];
}

export interface BadgeSummary {
  code: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  iconUrl: string | null;
  earnedAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class MeApiService {
  private readonly http = inject(HttpClient);

  get(): Promise<MeProfile> {
    return firstValueFrom(this.http.get<MeProfile>('/api/me'));
  }

  getBadges(): Promise<{ badges: BadgeSummary[] }> {
    return firstValueFrom(this.http.get<{ badges: BadgeSummary[] }>('/api/me/badges'));
  }
}

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { catchError, firstValueFrom, of } from 'rxjs';
import { PublicActivity, PublicActivityDetail } from './public-data.model';

@Injectable({ providedIn: 'root' })
export class PublicActivitiesApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<PublicActivity[]> {
    return firstValueFrom(this.http.get<PublicActivity[]>('/api/public/activities'));
  }

  getById(id: string): Promise<PublicActivityDetail | null> {
    return firstValueFrom(
      this.http.get<PublicActivityDetail>(`/api/public/activities/${id}`).pipe(catchError(() => of(null))),
    );
  }
}

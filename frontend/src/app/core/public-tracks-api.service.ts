import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { catchError, firstValueFrom, of } from 'rxjs';
import { PublicTrack, PublicTrackDetail } from './public-data.model';

@Injectable({ providedIn: 'root' })
export class PublicTracksApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<PublicTrack[]> {
    return firstValueFrom(this.http.get<PublicTrack[]>('/api/public/tracks'));
  }

  getById(id: string): Promise<PublicTrackDetail | null> {
    return firstValueFrom(
      this.http.get<PublicTrackDetail>(`/api/public/tracks/${id}`).pipe(catchError(() => of(null))),
    );
  }
}

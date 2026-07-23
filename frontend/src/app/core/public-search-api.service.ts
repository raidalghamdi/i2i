import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { catchError, firstValueFrom, of } from 'rxjs';
import { PublicSearchResults } from './public-data.model';

@Injectable({ providedIn: 'root' })
export class PublicSearchApiService {
  private readonly http = inject(HttpClient);

  search(q: string): Promise<PublicSearchResults> {
    const params = new HttpParams().set('q', q);
    return firstValueFrom(
      this.http
        .get<PublicSearchResults>('/api/public/search', { params })
        .pipe(catchError(() => of({ ideas: [], tracks: [] } as PublicSearchResults))),
    );
  }
}

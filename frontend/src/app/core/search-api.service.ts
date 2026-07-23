import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { SearchResults } from './search.model';

@Injectable({ providedIn: 'root' })
export class SearchApiService {
  private readonly http = inject(HttpClient);

  search(q: string): Promise<SearchResults> {
    if (q.trim() === '') {
      return Promise.resolve({ ideas: [], challenges: [], users: [] });
    }
    const params = new HttpParams().set('q', q);
    return firstValueFrom(this.http.get<SearchResults>('/api/search', { params }));
  }
}

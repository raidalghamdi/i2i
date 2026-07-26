import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface DirectoryPerson {
  samAccountName: string;
  displayName: string;
  email: string;
}

/** Wraps the backend AD-directory search endpoint (`GET /api/directory/search`),
 * used by `DirectoryPickerComponent` to look up people for team-member and
 * user-import pickers. */
@Injectable({ providedIn: 'root' })
export class DirectoryApiService {
  private readonly http = inject(HttpClient);

  search(q: string, limit = 10): Observable<DirectoryPerson[]> {
    const params = new HttpParams().set('q', q).set('limit', limit);
    return this.http.get<DirectoryPerson[]>('/api/directory/search', { params });
  }
}

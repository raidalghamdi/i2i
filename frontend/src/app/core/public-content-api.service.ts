import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { catchError, firstValueFrom, of } from 'rxjs';
import { PublicContent } from './public-content.model';

@Injectable({ providedIn: 'root' })
export class PublicContentApiService {
  private readonly http = inject(HttpClient);

  getBySlug(slug: string): Promise<PublicContent | null> {
    return firstValueFrom(
      this.http.get<PublicContent>(`/api/public/cms/content/${slug}`).pipe(catchError(() => of(null))),
    );
  }
}

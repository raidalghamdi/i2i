import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Activity } from './idea.model';

@Injectable({ providedIn: 'root' })
export class ActivitiesService {
  private readonly http = inject(HttpClient);

  list(): Promise<Activity[]> {
    return firstValueFrom(this.http.get<Activity[]>('/api/activities'));
  }
}

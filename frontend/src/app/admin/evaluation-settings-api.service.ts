import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { EvaluationSettings, EvaluationSettingsInput } from './evaluation-settings.model';

@Injectable({ providedIn: 'root' })
export class EvaluationSettingsApiService {
  private readonly http = inject(HttpClient);

  get(): Promise<EvaluationSettings> {
    return firstValueFrom(this.http.get<EvaluationSettings>('/api/admin/evaluation/settings'));
  }

  update(input: EvaluationSettingsInput): Promise<EvaluationSettings> {
    return firstValueFrom(this.http.patch<EvaluationSettings>('/api/admin/evaluation/settings', input));
  }
}

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { EvaluatorProductivityRow } from './evaluator-productivity.model';

// Change 20260726
@Injectable({ providedIn: 'root' })
export class EvaluatorProductivityApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<EvaluatorProductivityRow[]> {
    return firstValueFrom(
      this.http.get<EvaluatorProductivityRow[]>('/api/reports/evaluator-productivity'),
    );
  }
}

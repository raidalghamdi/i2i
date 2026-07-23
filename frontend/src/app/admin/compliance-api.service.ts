import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ComplianceControlRow } from './compliance.model';

@Injectable({ providedIn: 'root' })
export class ComplianceApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<ComplianceControlRow[]> {
    return firstValueFrom(this.http.get<ComplianceControlRow[]>('/api/admin/compliance'));
  }
}

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ExportService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/export`;

  startExport(entityType: string, format: 'csv' | 'excel') {
    return this.http.post<{ jobId: string, message: string }>(`${this.baseUrl}/${entityType}?format=${format}`, {});
  }
}

import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  IntegrationDto,
  IntegrationTestResult,
  IntegrationType,
  UpsertIntegrationDto,
} from '../../../shared/models/integration.models';

/**
 * Owns the tenant's integration collection for the settings screen. Mirrors the automation service:
 * a local signal cache keeps card status fresh after save/test without a full refetch.
 */
@Injectable({ providedIn: 'root' })
export class IntegrationsService {
  private readonly api = inject(ApiService);

  private readonly _integrations = signal<IntegrationDto[]>([]);
  readonly integrations = this._integrations.asReadonly();

  private readonly _isLoading = signal(false);
  readonly isLoading = this._isLoading.asReadonly();

  loadAll(): Observable<IntegrationDto[]> {
    this._isLoading.set(true);
    return this.api.get<IntegrationDto[]>('integrations').pipe(
      tap({
        next: (list) => {
          this._integrations.set(list);
          this._isLoading.set(false);
        },
        error: () => this._isLoading.set(false),
      }),
    );
  }

  upsert(type: IntegrationType, dto: UpsertIntegrationDto): Observable<IntegrationDto> {
    return this.api
      .put<IntegrationDto>(`integrations/${type}`, dto)
      .pipe(tap((updated) => this.replace(updated)));
  }

  test(type: IntegrationType): Observable<IntegrationTestResult> {
    return this.api.post<IntegrationTestResult>(`integrations/${type}/test`, {});
  }

  private replace(updated: IntegrationDto): void {
    this._integrations.update((list) =>
      list.map((i) => (i.type === updated.type ? { ...i, ...updated } : i)),
    );
  }
}

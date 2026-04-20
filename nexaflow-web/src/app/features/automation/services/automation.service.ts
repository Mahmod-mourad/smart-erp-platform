import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  CreateWorkflowRuleDto,
  UpdateWorkflowRuleDto,
  WorkflowLogPageDto,
  WorkflowRuleDto,
} from '../../../shared/models/automation.models';

/**
 * Owns the automation-rules collection for the list/builder screens. Keeps a local signal cache
 * so toggles/deletes reflect instantly without a full refetch, mirroring the inventory feature.
 */
@Injectable({ providedIn: 'root' })
export class AutomationService {
  private readonly api = inject(ApiService);

  private readonly _rules = signal<WorkflowRuleDto[]>([]);
  readonly rules = this._rules.asReadonly();

  private readonly _isLoading = signal(false);
  readonly isLoading = this._isLoading.asReadonly();

  loadAll(): Observable<WorkflowRuleDto[]> {
    this._isLoading.set(true);
    return this.api.get<WorkflowRuleDto[]>('automation/rules').pipe(
      tap({
        next: (rules) => {
          this._rules.set(rules);
          this._isLoading.set(false);
        },
        error: () => this._isLoading.set(false),
      }),
    );
  }

  getRule(id: string): Observable<WorkflowRuleDto> {
    return this.api.get<WorkflowRuleDto>(`automation/rules/${id}`);
  }

  create(dto: CreateWorkflowRuleDto): Observable<WorkflowRuleDto> {
    return this.api
      .post<WorkflowRuleDto>('automation/rules', dto)
      .pipe(tap((created) => this._rules.update((list) => [created, ...list])));
  }

  update(id: string, dto: UpdateWorkflowRuleDto): Observable<WorkflowRuleDto> {
    return this.api
      .put<WorkflowRuleDto>(`automation/rules/${id}`, dto)
      .pipe(tap((updated) => this.replace(updated)));
  }

  toggle(id: string): Observable<WorkflowRuleDto> {
    return this.api
      .patch<WorkflowRuleDto>(`automation/rules/${id}/toggle`, {})
      .pipe(tap((updated) => this.replace(updated)));
  }

  test(id: string): Observable<{ message: string }> {
    return this.api.post<{ message: string }>(`automation/rules/${id}/test`, {});
  }

  remove(id: string): Observable<void> {
    return this.api
      .delete(`automation/rules/${id}`)
      .pipe(tap(() => this._rules.update((list) => list.filter((r) => r.id !== id))));
  }

  getLogs(id: string, page = 1, pageSize = 10): Observable<WorkflowLogPageDto> {
    return this.api.get<WorkflowLogPageDto>(
      `automation/rules/${id}/logs?page=${page}&pageSize=${pageSize}`,
    );
  }

  private replace(updated: WorkflowRuleDto): void {
    this._rules.update((list) => list.map((r) => (r.id === updated.id ? updated : r)));
  }
}

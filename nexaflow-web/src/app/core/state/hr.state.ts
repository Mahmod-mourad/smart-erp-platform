import { Injectable, computed, signal } from '@angular/core';
import { EmployeeDto } from '../../shared/models/hr.models';

/** Shared HR state — employee list and detail read from the same signals. */
@Injectable({ providedIn: 'root' })
export class HrState {
  private readonly _employees = signal<EmployeeDto[]>([]);
  private readonly _isLoading = signal(false);
  private readonly _deptFilter = signal<string>('all');

  readonly employees = this._employees.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly deptFilter = this._deptFilter.asReadonly();

  readonly filteredEmployees = computed(() => {
    const dept = this._deptFilter();
    if (dept === 'all') return this._employees();
    return this._employees().filter((e) => e.department === dept);
  });

  /** Distinct departments present in the loaded employees — drives the filter dropdown. */
  readonly departments = computed(() =>
    [...new Set(this._employees().map((e) => e.department))].sort(),
  );

  setEmployees(data: EmployeeDto[]): void {
    this._employees.set(data);
  }

  setLoading(value: boolean): void {
    this._isLoading.set(value);
  }

  setDeptFilter(dept: string): void {
    this._deptFilter.set(dept);
  }

  addEmployee(employee: EmployeeDto): void {
    this._employees.update((list) => [employee, ...list]);
  }

  updateEmployee(updated: EmployeeDto): void {
    this._employees.update((list) => list.map((e) => (e.id === updated.id ? updated : e)));
  }

  removeEmployee(id: string): void {
    this._employees.update((list) => list.filter((e) => e.id !== id));
  }
}

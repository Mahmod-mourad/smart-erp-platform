import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { CurrencyPipe } from '@angular/common';
import { HrState } from '../../../../core/state/hr.state';
import { EmployeesService } from '../../services/employees.service';
import { EmployeeFormDialog } from '../employee-form-dialog/employee-form-dialog';
import { CreateEmployeeDto } from '../../../../shared/models/hr.models';

/** Employee grid with department filter and create dialog. */
@Component({
  selector: 'app-employee-list',
  imports: [
    CurrencyPipe, MatButtonModule, MatIconModule, MatFormFieldModule,
    MatSelectModule, MatProgressBarModule, MatDialogModule,
  ],
  template: `
    <div class="page-header">
      <h1>Employees</h1>
      <button mat-flat-button color="primary" (click)="openCreate()">
        <mat-icon>add</mat-icon> New Employee
      </button>
    </div>

    <div class="filters">
      <mat-form-field appearance="outline">
        <mat-label>Department</mat-label>
        <mat-select [value]="hr.deptFilter()" (selectionChange)="hr.setDeptFilter($event.value)">
          <mat-option value="all">All departments</mat-option>
          @for (d of hr.departments(); track d) {
            <mat-option [value]="d">{{ d }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
    </div>

    @if (hr.isLoading()) {
      <mat-progress-bar mode="indeterminate"></mat-progress-bar>
    }

    @if (!hr.isLoading() && hr.filteredEmployees().length === 0) {
      <div class="empty-state">
        <mat-icon>badge</mat-icon>
        <p>No employees yet.</p>
      </div>
    } @else {
      <div class="grid">
        @for (e of hr.filteredEmployees(); track e.id) {
          <div class="card" (click)="openDetail(e.id)">
            <div class="avatar">{{ initials(e.fullName) }}</div>
            <div class="info">
              <h3>{{ e.fullName }}</h3>
              <p class="muted">{{ e.position }} · {{ e.department }}</p>
              <p class="salary">{{ e.baseSalary | currency: 'EGP' }}</p>
            </div>
            <span class="status" [class]="e.status.toLowerCase()">{{ e.status }}</span>
          </div>
        }
      </div>
    }
  `,
  styles: `
    .page-header { display: flex; justify-content: space-between; align-items: center; padding: 24px 24px 0; }
    .filters { padding: 16px 24px 0; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 16px; padding: 16px 24px; }
    .card {
      display: flex; align-items: center; gap: 12px; padding: 16px; cursor: pointer;
      border: 1px solid var(--mat-sys-outline-variant); border-radius: 12px;
      background: var(--mat-sys-surface-container-low); transition: box-shadow .15s;
    }
    .card:hover { box-shadow: var(--mat-sys-level2); }
    .avatar {
      width: 48px; height: 48px; border-radius: 50%; flex: 0 0 48px;
      display: grid; place-items: center; font-weight: 600;
      background: var(--mat-sys-primary-container); color: var(--mat-sys-on-primary-container);
    }
    .info { flex: 1; min-width: 0; }
    .info h3 { margin: 0; font-size: 16px; }
    .muted { color: var(--mat-sys-on-surface-variant); margin: 2px 0; font-size: 13px; }
    .salary { margin: 2px 0 0; font-weight: 500; }
    .status { font-size: 12px; padding: 2px 8px; border-radius: 999px; align-self: flex-start; }
    .status.active { background: var(--mat-sys-tertiary-container); color: var(--mat-sys-on-tertiary-container); }
    .status.onleave { background: var(--mat-sys-secondary-container); color: var(--mat-sys-on-secondary-container); }
    .status.terminated { background: var(--mat-sys-error-container); color: var(--mat-sys-on-error-container); }
    .empty-state { display: grid; place-items: center; gap: 8px; padding: 64px; color: var(--mat-sys-on-surface-variant); }
  `,
})
export class EmployeeList {
  protected readonly hr = inject(HrState);
  private readonly employeesService = inject(EmployeesService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  constructor() {
    this.employeesService.loadAll().subscribe({ error: () => {} });
  }

  initials(name: string): string {
    return name.split(' ').map((p) => p[0]).slice(0, 2).join('').toUpperCase();
  }

  openDetail(id: string): void {
    this.router.navigate(['/hr', id]);
  }

  openCreate(): void {
    this.dialog
      .open(EmployeeFormDialog, { data: null })
      .afterClosed()
      .subscribe((result?: CreateEmployeeDto) => {
        if (result) this.employeesService.create(result).subscribe();
      });
  }
}

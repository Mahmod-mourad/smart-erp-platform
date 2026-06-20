import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { EmployeesService } from '../../services/employees.service';
import { EmployeeFormDialog } from '../employee-form-dialog/employee-form-dialog';
import { AttendanceCalendar } from '../../attendance/attendance-calendar/attendance-calendar';
import { PayslipPage } from '../../payroll/payslip-page/payslip-page';
import { EmployeeDto, UpdateEmployeeDto } from '../../../../shared/models/hr.models';

/** Employee detail with Info / Attendance / Payroll tabs. */
@Component({
  selector: 'app-employee-detail',
  imports: [
    RouterLink, CurrencyPipe, DatePipe, MatButtonModule, MatIconModule,
    MatTabsModule, MatDialogModule, AttendanceCalendar, PayslipPage,
  ],
  template: `
    <div class="page">
      <a mat-stroked-button routerLink="/hr"><mat-icon>arrow_back</mat-icon> Back</a>

      @if (employee(); as e) {
        <header class="head">
          <div>
            <h1>{{ e.fullName }}</h1>
            <p class="muted">{{ e.position }} · {{ e.department }}</p>
          </div>
          <button mat-flat-button color="primary" (click)="edit(e)">
            <mat-icon>edit</mat-icon> Edit
          </button>
        </header>

        <mat-tab-group>
          <mat-tab label="Info">
            <div class="tab info">
              <div class="field"><span>Email</span><strong>{{ e.email || '—' }}</strong></div>
              <div class="field"><span>Phone</span><strong>{{ e.phone || '—' }}</strong></div>
              <div class="field"><span>Hire date</span><strong>{{ e.hireDate | date: 'mediumDate' }}</strong></div>
              <div class="field"><span>Status</span><strong>{{ e.status }}</strong></div>
              <div class="field"><span>Base salary</span><strong>{{ e.baseSalary | currency: 'EGP' }}</strong></div>
              <div class="field"><span>Allowances</span><strong>{{ e.allowances | currency: 'EGP' }}</strong></div>
            </div>
          </mat-tab>
          <mat-tab label="Attendance">
            <div class="tab">
              <app-attendance-calendar [employeeId]="e.id" />
            </div>
          </mat-tab>
          <mat-tab label="Payroll">
            <div class="tab">
              <app-payslip-page [employeeId]="e.id" />
            </div>
          </mat-tab>
        </mat-tab-group>
      }
    </div>
  `,
  styles: `
    .page { padding: 24px; }
    .head { display: flex; justify-content: space-between; align-items: flex-start; margin: 16px 0; }
    .head h1 { margin: 0; }
    .muted { color: var(--mat-sys-on-surface-variant); margin: 4px 0 0; }
    .tab { padding: 24px 4px; }
    .info { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 16px; }
    .field { display: flex; flex-direction: column; gap: 2px; }
    .field span { font-size: 12px; color: var(--mat-sys-on-surface-variant); }
  `,
})
export class EmployeeDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly employeesService = inject(EmployeesService);
  private readonly dialog = inject(MatDialog);

  protected readonly employee = signal<EmployeeDto | null>(null);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.employeesService.getById(id).subscribe((e) => this.employee.set(e));
  }

  edit(employee: EmployeeDto): void {
    this.dialog
      .open(EmployeeFormDialog, { data: employee })
      .afterClosed()
      .subscribe((result?: UpdateEmployeeDto) => {
        if (result) {
          this.employeesService
            .update(employee.id, result)
            .subscribe((updated) => this.employee.set(updated));
        }
      });
  }
}

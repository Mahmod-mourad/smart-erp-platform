import { Component, inject, input, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { PayrollService } from '../../services/payroll.service';
import { PayslipDto } from '../../../../shared/models/hr.models';

/** Payslip viewer for one employee with month/year selectors and PDF download. */
@Component({
  selector: 'app-payslip-page',
  imports: [
    CurrencyPipe, MatButtonModule, MatIconModule, MatFormFieldModule,
    MatSelectModule, MatProgressBarModule,
  ],
  template: `
    <div class="controls">
      <mat-form-field appearance="outline">
        <mat-label>Month</mat-label>
        <mat-select [value]="month()" (selectionChange)="month.set($event.value)">
          @for (m of months; track m.value) {
            <mat-option [value]="m.value">{{ m.label }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>Year</mat-label>
        <mat-select [value]="year()" (selectionChange)="year.set($event.value)">
          @for (y of years; track y) { <mat-option [value]="y">{{ y }}</mat-option> }
        </mat-select>
      </mat-form-field>
      <button mat-flat-button color="primary" (click)="load()">Load</button>
    </div>

    @if (isLoading()) { <mat-progress-bar mode="indeterminate"></mat-progress-bar> }

    @if (payslip(); as p) {
      <div class="cards">
        <section class="card">
          <h3>Salary Breakdown</h3>
          <div class="row"><span>Base Salary</span><span>{{ p.baseSalary | currency: 'EGP' }}</span></div>
          <div class="row"><span>Allowances</span><span>{{ p.allowances | currency: 'EGP' }}</span></div>
          <div class="row total"><span>Gross Salary</span><span>{{ p.grossSalary | currency: 'EGP' }}</span></div>
          <div class="row deduction"><span>Absence Deduction</span><span>- {{ p.absenceDeduction | currency: 'EGP' }}</span></div>
          <div class="row net"><span>NET SALARY</span><span>{{ p.netSalary | currency: 'EGP' }}</span></div>
        </section>

        <section class="card">
          <h3>Attendance Summary</h3>
          <div class="summary">
            <div><strong>{{ p.presentDays }}</strong><span>Present</span></div>
            <div><strong>{{ p.absentDays }}</strong><span>Absent</span></div>
            <div><strong>{{ p.leaveDays }}</strong><span>Leave</span></div>
            <div><strong>{{ p.workingDays }}</strong><span>Working Days</span></div>
          </div>
          <button mat-flat-button color="primary" class="dl" (click)="download()">
            <mat-icon>download</mat-icon> Download PDF
          </button>
        </section>
      </div>
    }
  `,
  styles: `
    .controls { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; }
    .cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px; margin-top: 8px; }
    .card { padding: 16px; border: 1px solid var(--mat-sys-outline-variant); border-radius: 12px; }
    .card h3 { margin: 0 0 12px; }
    .row { display: flex; justify-content: space-between; padding: 6px 0; border-bottom: 1px solid var(--mat-sys-outline-variant); }
    .row.total { font-weight: 600; }
    .row.deduction { color: var(--mat-sys-error); }
    .row.net { font-weight: 700; font-size: 18px; border-bottom: none; }
    .summary { display: grid; grid-template-columns: repeat(4, 1fr); gap: 8px; text-align: center; margin-bottom: 16px; }
    .summary div { display: flex; flex-direction: column; }
    .summary strong { font-size: 22px; }
    .summary span { font-size: 12px; color: var(--mat-sys-on-surface-variant); }
    .dl { width: 100%; }
  `,
})
export class PayslipPage {
  readonly employeeId = input.required<string>();

  private readonly payrollService = inject(PayrollService);

  protected readonly months = Array.from({ length: 12 }, (_, i) => ({
    value: i + 1,
    label: new Date(2000, i, 1).toLocaleString('en-US', { month: 'long' }),
  }));
  protected readonly years = [2024, 2025, 2026, 2027];

  protected readonly year = signal(new Date().getFullYear());
  protected readonly month = signal(new Date().getMonth() + 1);
  protected readonly payslip = signal<PayslipDto | null>(null);
  protected readonly isLoading = signal(false);

  load(): void {
    this.isLoading.set(true);
    this.payrollService.getPayslip(this.employeeId(), this.year(), this.month()).subscribe({
      next: (data) => {
        this.payslip.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }

  download(): void {
    this.payrollService.downloadPdf(this.employeeId(), this.year(), this.month()).subscribe();
  }
}

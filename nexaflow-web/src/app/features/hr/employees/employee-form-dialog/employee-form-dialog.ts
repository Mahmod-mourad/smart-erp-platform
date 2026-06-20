import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { EmployeeDto } from '../../../../shared/models/hr.models';
import { BranchService } from '../../../../core/services/branch.service';

/** Create/edit an employee. Closes with the form payload (Create or Update shape) or undefined. */
@Component({
  selector: 'app-employee-form-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Edit Employee' : 'New Employee' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="grid">
        <mat-form-field appearance="outline">
          <mat-label>First name</mat-label>
          <input matInput formControlName="firstName" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Last name</mat-label>
          <input matInput formControlName="lastName" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Email</mat-label>
          <input matInput type="email" formControlName="email" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Phone</mat-label>
          <input matInput formControlName="phone" />
        </mat-form-field>
        @if (!data) {
          <mat-form-field appearance="outline">
            <mat-label>National ID</mat-label>
            <input matInput formControlName="nationalId" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Hire date</mat-label>
            <input matInput type="date" formControlName="hireDate" />
          </mat-form-field>
        }
        <mat-form-field appearance="outline">
          <mat-label>Branch</mat-label>
          <mat-select formControlName="branchId">
            <mat-option [value]="null">None</mat-option>
            @for (b of branchService.branches(); track b.id) {
              <mat-option [value]="b.id">{{ b.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Department</mat-label>
          <input matInput formControlName="department" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Position</mat-label>
          <input matInput formControlName="position" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Base salary</mat-label>
          <input matInput type="number" formControlName="baseSalary" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Allowances</mat-label>
          <input matInput type="number" formControlName="allowances" />
        </mat-form-field>
        @if (data) {
          <mat-form-field appearance="outline">
            <mat-label>Status</mat-label>
            <mat-select formControlName="status">
              @for (s of statuses; track s) {
                <mat-option [value]="s">{{ s }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        }
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="save()">Save</button>
    </mat-dialog-actions>
  `,
  styles: `
    .grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 8px 16px;
      min-width: 480px;
      padding-top: 8px;
    }
    mat-form-field { width: 100%; }
  `,
})
export class EmployeeFormDialog {
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject(MatDialogRef<EmployeeFormDialog>);
  protected readonly data = inject<EmployeeDto | null>(MAT_DIALOG_DATA);
  public readonly branchService = inject(BranchService);

  constructor() {
    this.branchService.loadBranches();
  }

  protected readonly statuses = ['Active', 'OnLeave', 'Terminated'];

  protected readonly form = this.fb.nonNullable.group({
    firstName: [this.data?.firstName ?? '', Validators.required],
    lastName: [this.data?.lastName ?? '', Validators.required],
    email: [this.data?.email ?? ''],
    phone: [this.data?.phone ?? ''],
    nationalId: [''],
    hireDate: [new Date().toISOString().slice(0, 10), Validators.required],
    department: [this.data?.department ?? '', Validators.required],
    position: [this.data?.position ?? '', Validators.required],
    baseSalary: [this.data?.baseSalary ?? 0, [Validators.required, Validators.min(0)]],
    allowances: [this.data?.allowances ?? 0, [Validators.required, Validators.min(0)]],
    status: [this.data?.status ?? 'Active'],
    branchId: [this.data?.branchId ?? null],
  });

  save(): void {
    this.ref.close(this.form.getRawValue());
  }
}

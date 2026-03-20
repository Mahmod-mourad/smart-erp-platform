import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { CustomerDto, CustomerStatus } from '../../../../shared/models/crm.models';

/** Create/edit a customer. Closes with the raw form value (Create or Update shape) or undefined. */
@Component({
  selector: 'app-customer-form-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Edit Customer' : 'New Customer' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="grid">
        <mat-form-field appearance="outline" class="span-2">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Email</mat-label>
          <input matInput type="email" formControlName="email" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Phone</mat-label>
          <input matInput formControlName="phone" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Company</mat-label>
          <input matInput formControlName="company" />
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
        <mat-form-field appearance="outline" class="span-2">
          <mat-label>Notes</mat-label>
          <textarea matInput rows="3" formControlName="notes"></textarea>
        </mat-form-field>
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
    .span-2 { grid-column: 1 / -1; }
    mat-form-field { width: 100%; }
  `,
})
export class CustomerFormDialog {
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject(MatDialogRef<CustomerFormDialog>);
  protected readonly data = inject<CustomerDto | null>(MAT_DIALOG_DATA);

  protected readonly statuses: CustomerStatus[] = ['Active', 'Inactive', 'Lead', 'Churned'];

  protected readonly form = this.fb.nonNullable.group({
    name: [this.data?.name ?? '', [Validators.required, Validators.maxLength(150)]],
    email: [this.data?.email ?? '', Validators.email],
    phone: [this.data?.phone ?? ''],
    company: [this.data?.company ?? ''],
    notes: [''],
    status: [this.data?.status ?? ('Active' as CustomerStatus)],
  });

  save(): void {
    this.ref.close(this.form.getRawValue());
  }
}

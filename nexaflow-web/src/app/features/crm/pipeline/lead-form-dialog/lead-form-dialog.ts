import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { CrmState } from '../../../../core/state/crm.state';
import { CreateLeadDto } from '../../../../shared/models/crm.models';

/** Create a new lead/opportunity. Closes with a CreateLeadDto or undefined. */
@Component({
  selector: 'app-lead-form-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>New Lead</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="grid">
        <mat-form-field appearance="outline" class="span-2">
          <mat-label>Title</mat-label>
          <input matInput formControlName="title" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="span-2">
          <mat-label>Customer</mat-label>
          <mat-select formControlName="customerId">
            @for (c of customers(); track c.id) {
              <mat-option [value]="c.id">{{ c.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Value (EGP)</mat-label>
          <input matInput type="number" formControlName="value" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Expected close</mat-label>
          <input matInput type="date" formControlName="expectedCloseDate" />
        </mat-form-field>
      </form>
      @if (customers().length === 0) {
        <p class="hint">Create a customer first — leads must belong to one.</p>
      }
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
    .hint { color: var(--mat-sys-error); margin: 0; }
  `,
})
export class LeadFormDialog {
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject(MatDialogRef<LeadFormDialog>);
  private readonly crmState = inject(CrmState);

  protected readonly customers = this.crmState.customers;

  protected readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(150)]],
    customerId: ['', Validators.required],
    value: [0, [Validators.required, Validators.min(0)]],
    expectedCloseDate: [''],
  });

  save(): void {
    const raw = this.form.getRawValue();
    const dto: CreateLeadDto = {
      title: raw.title,
      customerId: raw.customerId,
      value: raw.value,
      expectedCloseDate: raw.expectedCloseDate || undefined,
    };
    this.ref.close(dto);
  }
}

import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatRadioModule } from '@angular/material/radio';
import { ProductDto } from '../../../../shared/models/inventory.models';

/** Records a stock In/Out movement. Closes with an AddStockMovementDto or undefined. */
@Component({
  selector: 'app-stock-movement-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, MatRadioModule,
  ],
  template: `
    <h2 mat-dialog-title>Stock Movement — {{ data.name }}</h2>
    <mat-dialog-content>
      <p class="current">Current stock: <strong>{{ data.currentStock }}</strong></p>
      <form [formGroup]="form" class="form">
        <mat-radio-group formControlName="type" class="radios">
          <mat-radio-button value="In">Add stock</mat-radio-button>
          <mat-radio-button value="Out">Remove stock</mat-radio-button>
        </mat-radio-group>

        <mat-form-field appearance="outline">
          <mat-label>Quantity</mat-label>
          <input matInput type="number" formControlName="quantity" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Reason</mat-label>
          <input matInput formControlName="reason" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="save()">Save Movement</button>
    </mat-dialog-actions>
  `,
  styles: `
    .current { margin: 0 0 12px; }
    .form { display: flex; flex-direction: column; min-width: 360px; }
    .radios { display: flex; gap: 24px; margin-bottom: 16px; }
    mat-form-field { width: 100%; }
  `,
})
export class StockMovementDialog {
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject(MatDialogRef<StockMovementDialog>);
  protected readonly data = inject<ProductDto>(MAT_DIALOG_DATA);

  protected readonly form = this.fb.nonNullable.group({
    type: ['In' as 'In' | 'Out', Validators.required],
    quantity: [1, [Validators.required, Validators.min(1)]],
    reason: ['', Validators.required],
  });

  save(): void {
    this.ref.close(this.form.getRawValue());
  }
}

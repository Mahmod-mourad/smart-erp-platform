import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

/** Creates a product. Closes with a CreateProductDto or undefined. */
@Component({
  selector: 'app-product-form-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>New Product</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="grid">
        <mat-form-field appearance="outline" class="span2">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>SKU</mat-label>
          <input matInput formControlName="sku" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Category</mat-label>
          <input matInput formControlName="category" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Unit price</mat-label>
          <input matInput type="number" formControlName="unitPrice" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Minimum stock</mat-label>
          <input matInput type="number" formControlName="minimumStock" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="span2">
          <mat-label>Description</mat-label>
          <input matInput formControlName="description" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="save()">Save</button>
    </mat-dialog-actions>
  `,
  styles: `
    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 8px 16px; min-width: 480px; padding-top: 8px; }
    .span2 { grid-column: 1 / -1; }
    mat-form-field { width: 100%; }
  `,
})
export class ProductFormDialog {
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject(MatDialogRef<ProductFormDialog>);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    sku: [''],
    category: [''],
    unitPrice: [0, [Validators.required, Validators.min(0)]],
    minimumStock: [0, [Validators.required, Validators.min(0)]],
    description: [''],
  });

  save(): void {
    this.ref.close(this.form.getRawValue());
  }
}

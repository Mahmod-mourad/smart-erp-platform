import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import {
  INTEGRATION_META,
  IntegrationType,
  UpsertIntegrationDto,
} from '../../../shared/models/integration.models';

export interface IntegrationConfigDialogData {
  type: IntegrationType;
  isEnabled: boolean;
  isConfigured: boolean;
}

/**
 * Single configure dialog driven by {@link INTEGRATION_META} field metadata, so all four
 * integrations share one form. Secret fields start blank; leaving a secret blank keeps the value
 * already stored on the server. Closes with an {@link UpsertIntegrationDto} or undefined.
 */
@Component({
  selector: 'app-integration-config-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, MatSlideToggleModule,
  ],
  template: `
    <h2 mat-dialog-title>Configure {{ meta.name }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        @for (field of meta.fields; track field.key) {
          <mat-form-field appearance="outline">
            <mat-label>{{ field.label }}</mat-label>
            @if (field.textarea) {
              <textarea
                matInput
                rows="4"
                [formControlName]="field.key"
                [placeholder]="placeholder(field.secret)"
              ></textarea>
            } @else {
              <input
                matInput
                [type]="field.secret ? 'password' : 'text'"
                [formControlName]="field.key"
                [placeholder]="placeholder(field.secret)"
                autocomplete="off"
              />
            }
            @if (field.hint) { <mat-hint>{{ field.hint }}</mat-hint> }
          </mat-form-field>
        }

        <mat-slide-toggle formControlName="isEnabled">Enabled</mat-slide-toggle>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" (click)="save()">Save</button>
    </mat-dialog-actions>
  `,
  styles: `
    .form { display: flex; flex-direction: column; min-width: 380px; gap: 4px; }
    mat-form-field { width: 100%; }
    mat-slide-toggle { margin-top: 8px; }
  `,
})
export class IntegrationConfigDialog {
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject(MatDialogRef<IntegrationConfigDialog>);
  protected readonly data = inject<IntegrationConfigDialogData>(MAT_DIALOG_DATA);
  protected readonly meta = INTEGRATION_META[this.data.type];

  protected readonly form = this.fb.nonNullable.group({
    isEnabled: [this.data.isEnabled],
    ...Object.fromEntries(this.meta.fields.map((f) => [f.key, ['']])),
  });

  protected placeholder(secret?: boolean): string {
    return secret && this.data.isConfigured ? '•••••• (leave blank to keep)' : '';
  }

  save(): void {
    const raw = this.form.getRawValue() as Record<string, string | boolean>;
    const config: Record<string, string> = {};
    for (const field of this.meta.fields) {
      const value = raw[field.key];
      if (typeof value === 'string' && value.trim()) config[field.key] = value.trim();
    }
    const dto: UpsertIntegrationDto = { isEnabled: Boolean(raw['isEnabled']), config };
    this.ref.close(dto);
  }
}

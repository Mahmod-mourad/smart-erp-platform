import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { toSignal } from '@angular/core/rxjs-interop';
import { LeavesService } from '../../services/leaves.service';
import { CreateLeaveRequestDto, LeaveType } from '../../../../shared/models/hr.models';

/** Employee view: submit a leave request with a live working-day count. */
@Component({
  selector: 'app-leave-request-form',
  imports: [
    RouterLink, ReactiveFormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
  ],
  template: `
    <div class="page">
      <a mat-stroked-button routerLink="/hr/leaves"><mat-icon>arrow_back</mat-icon> Back</a>
      <h1>Apply for Leave</h1>

      <form [formGroup]="form" class="form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline">
          <mat-label>Leave type</mat-label>
          <mat-select formControlName="type">
            @for (t of types; track t) { <mat-option [value]="t">{{ t }}</mat-option> }
          </mat-select>
        </mat-form-field>

        <div class="dates">
          <mat-form-field appearance="outline">
            <mat-label>Start date</mat-label>
            <input matInput type="date" formControlName="startDate" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>End date</mat-label>
            <input matInput type="date" formControlName="endDate" />
          </mat-form-field>
        </div>

        <p class="total">Total: <strong>{{ workingDays() }}</strong> working day(s)</p>

        <mat-form-field appearance="outline">
          <mat-label>Reason</mat-label>
          <textarea matInput rows="3" formControlName="reason"></textarea>
        </mat-form-field>

        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">
          Submit Request
        </button>

        @if (successMessage()) { <p class="ok">{{ successMessage() }}</p> }
        @if (errorMessage()) { <p class="err">{{ errorMessage() }}</p> }
      </form>
    </div>
  `,
  styles: `
    .page { padding: 24px; max-width: 560px; }
    .form { display: flex; flex-direction: column; margin-top: 16px; }
    .dates { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    mat-form-field { width: 100%; }
    .total { margin: 4px 0 12px; }
    .ok { color: var(--mat-sys-tertiary); }
    .err { color: var(--mat-sys-error); }
  `,
})
export class LeaveRequestForm {
  private readonly fb = inject(FormBuilder);
  private readonly leavesService = inject(LeavesService);

  protected readonly types: LeaveType[] = ['Annual', 'Sick', 'Emergency', 'Unpaid'];
  protected readonly successMessage = signal('');
  protected readonly errorMessage = signal('');

  protected readonly form = this.fb.nonNullable.group({
    type: ['Annual' as LeaveType, Validators.required],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    reason: ['', [Validators.required, Validators.minLength(10)]],
  });

  private readonly value = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  protected readonly workingDays = computed(() => {
    const { startDate, endDate } = this.value();
    if (!startDate || !endDate) return 0;
    const start = new Date(startDate);
    const end = new Date(endDate);
    if (end < start) return 0;
    let count = 0;
    for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
      if (d.getDay() !== 5) count++; // skip Friday
    }
    return count;
  });

  submit(): void {
    this.successMessage.set('');
    this.errorMessage.set('');
    this.leavesService.create(this.form.getRawValue() as CreateLeaveRequestDto).subscribe({
      next: () => {
        this.successMessage.set('Leave request submitted successfully!');
        this.form.reset({ type: 'Annual', startDate: '', endDate: '', reason: '' });
      },
      error: (err) => this.errorMessage.set(err.error?.title ?? 'Could not submit the request.'),
    });
  }
}

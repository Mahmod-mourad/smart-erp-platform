import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';
import { AccountingService } from '../../../../core/services/accounting.service';

@Component({
  selector: 'app-journal-form-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatButtonModule, MatInputModule, MatSelectModule, MatIconModule, TranslatePipe],
  templateUrl: './journal-form-dialog.html',
  styles: [`
    .line-row { display: flex; gap: 10px; align-items: baseline; }
    .total-row { display: flex; justify-content: flex-end; gap: 20px; font-weight: bold; margin-top: 10px; }
    .error { color: red; margin-top: 10px; }
  `]
})
export class JournalFormDialog implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject(MatDialogRef<JournalFormDialog>);
  public accountingService = inject(AccountingService);

  form = this.fb.group({
    referenceNumber: ['', [Validators.required]],
    date: [new Date().toISOString().substring(0, 10), [Validators.required]],
    description: ['', [Validators.required]],
    lines: this.fb.array([])
  });

  get lines() {
    return this.form.get('lines') as FormArray;
  }

  ngOnInit() {
    this.accountingService.loadAccounts();
    this.addLine();
    this.addLine();
  }

  addLine() {
    this.lines.push(this.fb.group({
      accountId: ['', Validators.required],
      debit: [0, [Validators.min(0)]],
      credit: [0, [Validators.min(0)]],
      description: ['']
    }));
  }

  removeLine(index: number) {
    this.lines.removeAt(index);
  }

  get totalDebit() {
    return this.lines.controls.reduce((sum, ctrl) => sum + (ctrl.value.debit || 0), 0);
  }

  get totalCredit() {
    return this.lines.controls.reduce((sum, ctrl) => sum + (ctrl.value.credit || 0), 0);
  }

  get isBalanced() {
    const debit = this.totalDebit;
    const credit = this.totalCredit;
    return debit === credit && debit > 0;
  }

  save() {
    if (this.form.valid && this.isBalanced) {
      this.ref.close(this.form.value);
    }
  }
}

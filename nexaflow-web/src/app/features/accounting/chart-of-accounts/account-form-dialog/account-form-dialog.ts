import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { TranslatePipe } from '@ngx-translate/core';
import { AccountDto, AccountType } from '../../../../shared/models/accounting.models';

@Component({
  selector: 'app-account-form-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatButtonModule, MatInputModule, MatSelectModule, TranslatePipe],
  templateUrl: './account-form-dialog.html'
})
export class AccountFormDialog {
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject(MatDialogRef<AccountFormDialog>);

  form = this.fb.group({
    code: ['', [Validators.required]],
    name: ['', [Validators.required]],
    type: [AccountType.Asset, [Validators.required]],
    parentAccountId: [null]
  });

  accountTypes = [
    { value: AccountType.Asset, label: 'Asset' },
    { value: AccountType.Liability, label: 'Liability' },
    { value: AccountType.Equity, label: 'Equity' },
    { value: AccountType.Revenue, label: 'Revenue' },
    { value: AccountType.Expense, label: 'Expense' }
  ];

  save() {
    if (this.form.valid) {
      this.ref.close(this.form.value);
    }
  }
}

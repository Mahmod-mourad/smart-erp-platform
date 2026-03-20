import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { AccountingService } from '../../../core/services/accounting.service';
import { AccountFormDialog } from './account-form-dialog/account-form-dialog';

@Component({
  selector: 'app-chart-of-accounts',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatTableModule, MatDialogModule, TranslatePipe],
  templateUrl: './chart-of-accounts.html'
})
export class ChartOfAccounts implements OnInit {
  public accountingService = inject(AccountingService);
  private dialog = inject(MatDialog);

  displayedColumns = ['code', 'name', 'type', 'balance'];

  ngOnInit() {
    this.accountingService.loadAccounts();
  }

  openDialog() {
    const ref = this.dialog.open(AccountFormDialog, {
      width: '500px'
    });

    ref.afterClosed().subscribe(res => {
      if (res) {
        this.accountingService.createAccount(res).subscribe(() => this.accountingService.loadAccounts());
      }
    });
  }

  getAccountTypeName(type: number): string {
    switch(type) {
      case 1: return 'Asset';
      case 2: return 'Liability';
      case 3: return 'Equity';
      case 4: return 'Revenue';
      case 5: return 'Expense';
      default: return 'Unknown';
    }
  }
}

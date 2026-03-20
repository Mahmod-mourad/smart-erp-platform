import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { AccountingService } from '../../../core/services/accounting.service';
import { JournalFormDialog } from './journal-form-dialog/journal-form-dialog';

@Component({
  selector: 'app-journal-list',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatTableModule, MatDialogModule, TranslatePipe],
  templateUrl: './journal-list.html'
})
export class JournalList implements OnInit {
  public accountingService = inject(AccountingService);
  private dialog = inject(MatDialog);

  displayedColumns = ['referenceNumber', 'date', 'description', 'status'];

  ngOnInit() {
    this.accountingService.loadJournals();
  }

  openDialog() {
    const ref = this.dialog.open(JournalFormDialog, {
      width: '800px'
    });

    ref.afterClosed().subscribe(res => {
      if (res) {
        this.accountingService.postJournal(res).subscribe(() => this.accountingService.loadJournals());
      }
    });
  }

  getStatusName(status: number): string {
    switch(status) {
      case 1: return 'Draft';
      case 2: return 'Posted';
      case 3: return 'Void';
      default: return 'Unknown';
    }
  }
}

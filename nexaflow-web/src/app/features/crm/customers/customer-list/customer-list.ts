import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ExportService } from '../../../../core/services/export.service';
import { CrmState } from '../../../../core/state/crm.state';
import { CustomersService } from '../../services/customers.service';
import { CustomerFormDialog } from '../customer-form-dialog/customer-form-dialog';
import {
  CreateCustomerDto,
  CustomerDto,
  CustomerStatus,
  UpdateCustomerDto,
} from '../../../../shared/models/crm.models';
import { ConfirmDialog, ConfirmDialogData } from '../../../../shared/confirm-dialog/confirm-dialog';

/** The form shape returned by CustomerFormDialog (create ignores status). */
type CustomerFormResult = CreateCustomerDto & { status: CustomerStatus };

/** Step 5: customer table with search/filter, status badges and create/edit/delete. */
@Component({
  selector: 'app-customer-list',
  imports: [
    MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule,
    MatIconModule, MatMenuModule, MatProgressBarModule, MatDialogModule, MatSnackBarModule,
  ],
  templateUrl: './customer-list.html',
  styleUrl: './customer-list.scss',
})
export class CustomerList {
  private readonly customersService = inject(CustomersService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly exportService = inject(ExportService);
  private readonly snackBar = inject(MatSnackBar);
  protected readonly crmState = inject(CrmState);

  protected readonly customers = this.crmState.filteredCustomers;
  protected readonly statuses: (CustomerStatus | 'all')[] = [
    'all', 'Active', 'Inactive', 'Lead', 'Churned',
  ];

  constructor() {
    this.customersService.loadAll().subscribe({ error: () => {} });
  }

  onSearch(query: string): void {
    this.crmState.setSearch(query);
  }

  onFilterChange(status: CustomerStatus | 'all'): void {
    this.crmState.setFilter(status);
  }

  openDetail(id: string): void {
    this.router.navigate(['/crm', id]);
  }

  openCreate(): void {
    this.dialog
      .open(CustomerFormDialog, { data: null })
      .afterClosed()
      .subscribe((result?: CustomerFormResult) => {
        if (!result) return;
        const { status, ...create } = result;
        this.customersService.create(create).subscribe();
      });
  }

  openEdit(customer: CustomerDto): void {
    this.dialog
      .open(CustomerFormDialog, { data: customer })
      .afterClosed()
      .subscribe((result?: CustomerFormResult) => {
        if (!result) return;
        const payload: UpdateCustomerDto = { ...result };
        this.customersService.update(customer.id, payload).subscribe();
      });
  }

  confirmDelete(customer: CustomerDto): void {
    const data: ConfirmDialogData = {
      title: 'Delete customer',
      message: `Delete "${customer.name}"? Their leads and activity history will be removed too.`,
      confirmText: 'Delete',
      destructive: true,
    };
    this.dialog
      .open(ConfirmDialog, { data })
      .afterClosed()
      .subscribe((confirmed?: boolean) => {
        if (confirmed) this.customersService.delete(customer.id).subscribe();
      });
  }

  exportData(format: 'csv' | 'excel'): void {
    this.exportService.startExport('customers', format).subscribe({
      next: (res) => {
        this.snackBar.open(res.message, 'Close', { duration: 5000 });
      },
      error: (err) => {
        this.snackBar.open('Failed to start export', 'Close', { duration: 3000 });
      }
    });
  }
}

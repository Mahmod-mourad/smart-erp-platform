import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDialog } from '@angular/material/dialog';
import { CrmState } from '../../../../core/state/crm.state';
import { CustomersService } from '../../services/customers.service';
import { LeadsService } from '../../services/leads.service';
import { ActivitiesService } from '../../services/activities.service';
import { CustomerFormDialog } from '../customer-form-dialog/customer-form-dialog';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import {
  ActivityDto,
  ActivityType,
  CustomerDto,
  ManualActivityType,
  UpdateCustomerDto,
} from '../../../../shared/models/crm.models';

/** Step 6: customer profile, related leads and a reverse-chronological activity timeline. */
@Component({
  selector: 'app-customer-detail',
  imports: [
    RouterLink,
    DatePipe,
    CurrencyPipe,
    RelativeTimePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressBarModule,
  ],
  templateUrl: './customer-detail.html',
  styleUrl: './customer-detail.scss',
})
export class CustomerDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);
  private readonly customersService = inject(CustomersService);
  private readonly leadsService = inject(LeadsService);
  private readonly activitiesService = inject(ActivitiesService);
  private readonly crmState = inject(CrmState);

  protected readonly customerId = this.route.snapshot.paramMap.get('id')!;

  protected readonly customer = signal<CustomerDto | null>(null);
  protected readonly activities = signal<ActivityDto[]>([]);
  protected readonly isLoading = signal(true);

  /** Leads belonging to this customer, read from the shared CRM state. */
  protected readonly leads = computed(() =>
    this.crmState.leads().filter((l) => l.customerId === this.customerId),
  );

  protected readonly activityTypes: ManualActivityType[] = ['Note', 'Call', 'Email', 'Meeting'];

  protected readonly form = this.fb.nonNullable.group({
    type: ['Note' as ManualActivityType, Validators.required],
    content: ['', [Validators.required, Validators.maxLength(2000)]],
  });

  constructor() {
    this.customersService.getById(this.customerId).subscribe({
      next: (c) => {
        this.customer.set(c);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });

    // Leads power the related-leads panel; load them if the user landed here directly.
    if (this.crmState.leads().length === 0) {
      this.leadsService.loadAll().subscribe({ error: () => {} });
    }

    this.activitiesService.getForCustomer(this.customerId).subscribe({
      next: (list) => this.activities.set(list),
      error: () => {},
    });
  }

  addActivity(): void {
    if (this.form.invalid) return;
    const payload = this.form.getRawValue();
    this.activitiesService.create(this.customerId, payload).subscribe((created) => {
      this.activities.update((list) => [created, ...list]);
      this.form.reset({ type: 'Note', content: '' });
    });
  }

  editCustomer(): void {
    const current = this.customer();
    if (!current) return;
    this.dialog
      .open(CustomerFormDialog, { data: current })
      .afterClosed()
      .subscribe((result?: UpdateCustomerDto) => {
        if (!result) return;
        this.customersService
          .update(current.id, result)
          .subscribe((updated) => this.customer.set(updated));
      });
  }

  back(): void {
    this.router.navigate(['/crm']);
  }

  icon(type: ActivityType): string {
    switch (type) {
      case 'Call':
        return 'call';
      case 'Email':
        return 'mail';
      case 'Meeting':
        return 'groups';
      case 'StatusChange':
        return 'timeline';
      default:
        return 'sticky_note_2';
    }
  }
}

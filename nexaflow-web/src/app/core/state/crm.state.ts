import { Injectable, computed, signal } from '@angular/core';
import { CustomerDto, CustomerStatus, LeadDto, LeadStage } from '../../shared/models/crm.models';

/**
 * Shared CRM state. Customer List, Customer Detail and the Kanban board all read from the
 * same signals so a change in one place is reflected everywhere without extra requests.
 */
@Injectable({ providedIn: 'root' })
export class CrmState {
  private readonly _customers = signal<CustomerDto[]>([]);
  private readonly _leads = signal<LeadDto[]>([]);
  private readonly _searchQuery = signal('');
  private readonly _statusFilter = signal<CustomerStatus | 'all'>('all');
  private readonly _isLoading = signal(false);
  private readonly _selectedId = signal<string | null>(null);

  readonly customers = this._customers.asReadonly();
  readonly leads = this._leads.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly searchQuery = this._searchQuery.asReadonly();
  readonly statusFilter = this._statusFilter.asReadonly();
  readonly selectedId = this._selectedId.asReadonly();

  readonly filteredCustomers = computed(() => {
    const status = this._statusFilter();
    const query = this._searchQuery().trim().toLowerCase();

    return this._customers().filter((c) => {
      if (status !== 'all' && c.status !== status) return false;
      if (!query) return true;
      return (
        c.name.toLowerCase().includes(query) ||
        (c.email?.toLowerCase().includes(query) ?? false) ||
        (c.company?.toLowerCase().includes(query) ?? false)
      );
    });
  });

  /** Leads grouped by pipeline stage — consumed by the Kanban board. */
  readonly leadsByStage = computed(() => {
    const map = new Map<LeadStage, LeadDto[]>();
    for (const lead of this._leads()) {
      const bucket = map.get(lead.stage) ?? [];
      bucket.push(lead);
      map.set(lead.stage, bucket);
    }
    return map;
  });

  setCustomers(data: CustomerDto[]): void {
    this._customers.set(data);
  }

  setLeads(data: LeadDto[]): void {
    this._leads.set(data);
  }

  setLoading(value: boolean): void {
    this._isLoading.set(value);
  }

  setSearch(query: string): void {
    this._searchQuery.set(query);
  }

  setFilter(filter: CustomerStatus | 'all'): void {
    this._statusFilter.set(filter);
  }

  setSelected(id: string | null): void {
    this._selectedId.set(id);
  }

  addCustomer(customer: CustomerDto): void {
    this._customers.update((list) => [customer, ...list]);
  }

  updateCustomer(updated: CustomerDto): void {
    this._customers.update((list) => list.map((c) => (c.id === updated.id ? updated : c)));
  }

  removeCustomer(id: string): void {
    this._customers.update((list) => list.filter((c) => c.id !== id));
  }

  updateLeadStage(leadId: string, newStage: LeadStage): void {
    this._leads.update((list) =>
      list.map((l) => (l.id === leadId ? { ...l, stage: newStage } : l)),
    );
  }
}

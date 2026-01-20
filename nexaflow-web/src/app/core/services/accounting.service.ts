import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AccountDto, CreateAccountDto, JournalEntryDto, CreateJournalEntryDto } from '../../shared/models/accounting.models';

@Injectable({ providedIn: 'root' })
export class AccountingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/accounting`;

  public accounts = signal<AccountDto[]>([]);
  public journals = signal<JournalEntryDto[]>([]);
  public loading = signal(false);

  loadAccounts() {
    this.loading.set(true);
    this.http.get<AccountDto[]>(`${this.baseUrl}/accounts`).subscribe({
      next: (res) => {
        this.accounts.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  createAccount(dto: CreateAccountDto) {
    return this.http.post<AccountDto>(`${this.baseUrl}/accounts`, dto);
  }

  loadJournals() {
    this.loading.set(true);
    this.http.get<JournalEntryDto[]>(`${this.baseUrl}/journals`).subscribe({
      next: (res) => {
        this.journals.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  postJournal(dto: CreateJournalEntryDto) {
    return this.http.post<JournalEntryDto>(`${this.baseUrl}/journals`, dto);
  }
}

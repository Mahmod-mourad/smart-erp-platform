export enum AccountType {
  Asset = 1,
  Liability = 2,
  Equity = 3,
  Revenue = 4,
  Expense = 5
}

export enum JournalEntryStatus {
  Draft = 1,
  Posted = 2,
  Void = 3
}

export interface AccountDto {
  id: string;
  code: string;
  name: string;
  type: AccountType;
  parentAccountId?: string;
  balance: number;
}

export interface CreateAccountDto {
  code: string;
  name: string;
  type: AccountType;
  parentAccountId?: string;
}

export interface JournalEntryLineDto {
  accountId: string;
  debit: number;
  credit: number;
  description?: string;
}

export interface JournalEntryDto {
  id: string;
  referenceNumber: string;
  date: string;
  description: string;
  status: JournalEntryStatus;
  lines: JournalEntryLineDto[];
}

export interface CreateJournalEntryDto {
  referenceNumber: string;
  date: string;
  description: string;
  lines: JournalEntryLineDto[];
}

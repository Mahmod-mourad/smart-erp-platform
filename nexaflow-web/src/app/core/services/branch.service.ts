import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { catchError, tap } from 'rxjs/operators';
import { of } from 'rxjs';

export interface Branch {
  id: string;
  name: string;
  address?: string;
  city?: string;
  phone?: string;
  isHeadquarters: boolean;
}

export interface CreateBranchDto {
  name: string;
  address?: string;
  city?: string;
  phone?: string;
  isHeadquarters: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class BranchService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/branches`;

  // Signal state
  readonly branches = signal<Branch[]>([]);
  readonly loading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  loadBranches() {
    this.loading.set(true);
    this.error.set(null);
    this.http.get<Branch[]>(this.baseUrl).pipe(
      tap(data => this.branches.set(data)),
      catchError(err => {
        this.error.set('Failed to load branches.');
        return of([]);
      })
    ).subscribe(() => this.loading.set(false));
  }

  createBranch(dto: CreateBranchDto) {
    return this.http.post<Branch>(this.baseUrl, dto).pipe(
      tap(newBranch => this.branches.update(b => [...b, newBranch]))
    );
  }

  updateBranch(id: string, dto: CreateBranchDto) {
    return this.http.put<Branch>(`${this.baseUrl}/${id}`, dto).pipe(
      tap(updated => this.branches.update(b => b.map(x => x.id === id ? updated : x)))
    );
  }

  deleteBranch(id: string) {
    return this.http.delete(`${this.baseUrl}/${id}`).pipe(
      tap(() => this.branches.update(b => b.filter(x => x.id !== id)))
    );
  }
}

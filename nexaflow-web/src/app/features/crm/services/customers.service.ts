import { Injectable, inject } from '@angular/core';
import { Observable, catchError, tap, throwError } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { CrmState } from '../../../core/state/crm.state';
import {
  CreateCustomerDto,
  CustomerDto,
  UpdateCustomerDto,
} from '../../../shared/models/crm.models';

@Injectable({ providedIn: 'root' })
export class CustomersService {
  private readonly api = inject(ApiService);
  private readonly state = inject(CrmState);

  loadAll(): Observable<CustomerDto[]> {
    this.state.setLoading(true);
    return this.api.get<CustomerDto[]>('customers').pipe(
      tap((data) => {
        this.state.setCustomers(data);
        this.state.setLoading(false);
      }),
      catchError((err) => {
        this.state.setLoading(false);
        return throwError(() => err);
      }),
    );
  }

  getById(id: string): Observable<CustomerDto> {
    return this.api.get<CustomerDto>(`customers/${id}`);
  }

  create(dto: CreateCustomerDto): Observable<CustomerDto> {
    return this.api
      .post<CustomerDto>('customers', dto)
      .pipe(tap((created) => this.state.addCustomer(created)));
  }

  update(id: string, dto: UpdateCustomerDto): Observable<CustomerDto> {
    return this.api
      .put<CustomerDto>(`customers/${id}`, dto)
      .pipe(tap((updated) => this.state.updateCustomer(updated)));
  }

  delete(id: string): Observable<void> {
    return this.api.delete(`customers/${id}`).pipe(tap(() => this.state.removeCustomer(id)));
  }
}

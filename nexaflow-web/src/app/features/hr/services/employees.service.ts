import { Injectable, inject } from '@angular/core';
import { Observable, catchError, tap, throwError } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { HrState } from '../../../core/state/hr.state';
import {
  CreateEmployeeDto,
  EmployeeDto,
  UpdateEmployeeDto,
} from '../../../shared/models/hr.models';

@Injectable({ providedIn: 'root' })
export class EmployeesService {
  private readonly api = inject(ApiService);
  private readonly state = inject(HrState);

  loadAll(): Observable<EmployeeDto[]> {
    this.state.setLoading(true);
    return this.api.get<EmployeeDto[]>('employees').pipe(
      tap((data) => {
        this.state.setEmployees(data);
        this.state.setLoading(false);
      }),
      catchError((err) => {
        this.state.setLoading(false);
        return throwError(() => err);
      }),
    );
  }

  getById(id: string): Observable<EmployeeDto> {
    return this.api.get<EmployeeDto>(`employees/${id}`);
  }

  create(dto: CreateEmployeeDto): Observable<EmployeeDto> {
    return this.api
      .post<EmployeeDto>('employees', dto)
      .pipe(tap((created) => this.state.addEmployee(created)));
  }

  update(id: string, dto: UpdateEmployeeDto): Observable<EmployeeDto> {
    return this.api
      .put<EmployeeDto>(`employees/${id}`, dto)
      .pipe(tap((updated) => this.state.updateEmployee(updated)));
  }

  delete(id: string): Observable<void> {
    return this.api.delete(`employees/${id}`).pipe(tap(() => this.state.removeEmployee(id)));
  }
}

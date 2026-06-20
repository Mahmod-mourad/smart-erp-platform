import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { skipErrorNotification } from '../../../core/interceptors/error.interceptor';
import {
  CreateLeaveRequestDto,
  LeaveRequestDto,
  ReviewLeaveDto,
} from '../../../shared/models/hr.models';

@Injectable({ providedIn: 'root' })
export class LeavesService {
  private readonly api = inject(ApiService);

  // The leave-request form shows submission errors inline; list/review rely on the global toast.
  create(dto: CreateLeaveRequestDto): Observable<LeaveRequestDto> {
    return this.api.post<LeaveRequestDto>('leaves', dto, skipErrorNotification());
  }

  getAll(status?: string): Observable<LeaveRequestDto[]> {
    const query = status && status !== 'All' ? `?status=${status}` : '';
    return this.api.get<LeaveRequestDto[]>(`leaves${query}`);
  }

  review(id: string, dto: ReviewLeaveDto): Observable<LeaveRequestDto> {
    return this.api.patch<LeaveRequestDto>(`leaves/${id}/review`, dto);
  }
}

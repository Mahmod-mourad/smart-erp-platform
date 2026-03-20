import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ActivityDto, CreateActivityDto } from '../../../shared/models/crm.models';

/**
 * Customer activity timeline. Activities are always scoped to a single customer, so this
 * service is stateless (the Customer Detail page owns the loaded list locally) — unlike the
 * shared-signal CustomersService / LeadsService.
 */
@Injectable({ providedIn: 'root' })
export class ActivitiesService {
  private readonly api = inject(ApiService);

  getForCustomer(customerId: string): Observable<ActivityDto[]> {
    return this.api.get<ActivityDto[]>(`customers/${customerId}/activities`);
  }

  create(customerId: string, dto: CreateActivityDto): Observable<ActivityDto> {
    return this.api.post<ActivityDto>(`customers/${customerId}/activities`, dto);
  }
}

import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { CrmState } from '../../../core/state/crm.state';
import { CreateLeadDto, LeadDto, LeadStage } from '../../../shared/models/crm.models';

@Injectable({ providedIn: 'root' })
export class LeadsService {
  private readonly api = inject(ApiService);
  private readonly state = inject(CrmState);

  loadAll(): Observable<LeadDto[]> {
    return this.api.get<LeadDto[]>('leads').pipe(tap((data) => this.state.setLeads(data)));
  }

  create(dto: CreateLeadDto): Observable<LeadDto> {
    return this.api.post<LeadDto>('leads', dto);
  }

  updateStage(id: string, stage: LeadStage): Observable<LeadDto> {
    return this.api
      .patch<LeadDto>(`leads/${id}/stage`, { stage })
      .pipe(tap(() => this.state.updateLeadStage(id, stage)));
  }
}

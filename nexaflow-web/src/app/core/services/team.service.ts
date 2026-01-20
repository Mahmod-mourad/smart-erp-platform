import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { InviteMemberRequest } from '../models/auth.models';
import { skipErrorNotification } from '../interceptors/error.interceptor';

export interface InvitationDto {
  id: string;
  email: string;
  roleName: string;
  status: number;
  expiresAt: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class TeamService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/invitations`;

  // The team page surfaces failures via its own snackbar, so these calls opt out of the
  // global error toast.
  getPending(): Observable<InvitationDto[]> {
    return this.http.get<InvitationDto[]>(this.base, { context: skipErrorNotification() });
  }

  invite(body: InviteMemberRequest): Observable<InvitationDto> {
    return this.http.post<InvitationDto>(`${this.base}/`, body, {
      context: skipErrorNotification(),
    });
  }

  revoke(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`, { context: skipErrorNotification() });
  }
}

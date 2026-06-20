import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { AttendanceDto, AttendanceSummaryDto } from '../../../shared/models/hr.models';

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly api = inject(ApiService);

  checkIn(employeeId: string): Observable<AttendanceDto> {
    return this.api.post<AttendanceDto>('attendance/check-in', { employeeId });
  }

  checkOut(attendanceRecordId: string): Observable<AttendanceDto> {
    return this.api.post<AttendanceDto>('attendance/check-out', { attendanceRecordId });
  }

  getEmployeeMonth(employeeId: string, year: number, month: number): Observable<AttendanceDto[]> {
    return this.api.get<AttendanceDto[]>(`attendance/employee/${employeeId}?year=${year}&month=${month}`);
  }

  getDailySummary(date: string): Observable<AttendanceSummaryDto> {
    return this.api.get<AttendanceSummaryDto>(`attendance/summary?date=${date}`);
  }
}

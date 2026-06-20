import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { environment } from '../../../../environments/environment';
import { PayslipDto } from '../../../shared/models/hr.models';

@Injectable({ providedIn: 'root' })
export class PayrollService {
  private readonly api = inject(ApiService);
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getPayslip(employeeId: string, year: number, month: number): Observable<PayslipDto> {
    return this.api.get<PayslipDto>(`payroll/${employeeId}?year=${year}&month=${month}`);
  }

  /** Streams the payslip PDF as a blob and triggers a browser download. */
  downloadPdf(employeeId: string, year: number, month: number): Observable<Blob> {
    const url = `${this.baseUrl}/payroll/${employeeId}/pdf?year=${year}&month=${month}`;
    return this.http.get(url, { responseType: 'blob' }).pipe(
      tap((blob) => {
        const objectUrl = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = objectUrl;
        link.download = `payslip_${employeeId}_${year}_${String(month).padStart(2, '0')}.pdf`;
        link.click();
        URL.revokeObjectURL(objectUrl);
      }),
    );
  }
}

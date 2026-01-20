import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

/**
 * Thin typed wrapper over HttpClient that prefixes every call with the API base URL.
 * Feature services build on this instead of repeating the base-url plumbing.
 *
 * An optional `context` lets a caller attach per-request flags (e.g.
 * {@link skipErrorNotification}) when it wants to own the error UX itself.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  get<T>(endpoint: string, context?: HttpContext): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}/${endpoint}`, { context });
  }

  post<T>(endpoint: string, body: unknown, context?: HttpContext): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}/${endpoint}`, body, { context });
  }

  put<T>(endpoint: string, body: unknown, context?: HttpContext): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}/${endpoint}`, body, { context });
  }

  patch<T>(endpoint: string, body: unknown, context?: HttpContext): Observable<T> {
    return this.http.patch<T>(`${this.baseUrl}/${endpoint}`, body, { context });
  }

  delete<T = void>(endpoint: string, context?: HttpContext): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}/${endpoint}`, { context });
  }
}

import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { skipErrorNotification } from '../interceptors/error.interceptor';
import {
  AcceptInvitationRequest,
  AuthResponse,
  LoginRequest,
  RegisterCompanyRequest,
  UserDto,
} from '../models/auth.models';

const ACCESS_KEY = 'nf_access_token';
const REFRESH_KEY = 'nf_refresh_token';
const USER_KEY = 'nf_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/auth`;

  private readonly _user = signal<UserDto | null>(this.loadUser());
  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly roles = computed(() => this._user()?.roles ?? []);

  // Auth screens render validation/credential errors inline, so every call opts out of the
  // global error toast to avoid duplicate messaging.
  registerCompany(body: RegisterCompanyRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.base}/register-company`, body, {
        context: skipErrorNotification(),
      })
      .pipe(tap((res) => this.persist(res)));
  }

  login(body: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.base}/login`, body, { context: skipErrorNotification() })
      .pipe(tap((res) => this.persist(res)));
  }

  acceptInvite(body: AcceptInvitationRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.base}/accept-invite`, body, { context: skipErrorNotification() })
      .pipe(tap((res) => this.persist(res)));
  }

  refresh(): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(
        `${this.base}/refresh`,
        { refreshToken: this.refreshToken },
        { context: skipErrorNotification() },
      )
      .pipe(tap((res) => this.persist(res)));
  }

  logout(): void {
    const token = this.refreshToken;
    if (token) {
      this.http
        .post(`${this.base}/logout`, { refreshToken: token }, { context: skipErrorNotification() })
        .subscribe({ error: () => {} });
    }
    this.clear();
  }

  hasRole(role: string): boolean {
    return this.roles().includes(role);
  }

  get accessToken(): string | null {
    return localStorage.getItem(ACCESS_KEY);
  }

  get refreshToken(): string | null {
    return localStorage.getItem(REFRESH_KEY);
  }

  private persist(res: AuthResponse): void {
    localStorage.setItem(ACCESS_KEY, res.accessToken);
    localStorage.setItem(REFRESH_KEY, res.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(res.user));
    this._user.set(res.user);
  }

  private clear(): void {
    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(USER_KEY);
    this._user.set(null);
  }

  private loadUser(): UserDto | null {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as UserDto) : null;
  }
}

import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { RoleDto, CreateRoleDto, UpdateRoleDto } from '../../shared/models/role.models';

@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/roles`;

  public roles = signal<RoleDto[]>([]);
  public permissionsList = signal<string[]>([]);
  public loading = signal(false);

  loadRoles() {
    this.loading.set(true);
    this.http.get<RoleDto[]>(this.baseUrl).subscribe({
      next: (res) => {
        this.roles.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  loadPermissions() {
    this.http.get<string[]>(`${environment.apiBaseUrl}/api/permissions`).subscribe({
      next: (res) => this.permissionsList.set(res)
    });
  }

  create(dto: CreateRoleDto) {
    return this.http.post<RoleDto>(this.baseUrl, dto);
  }

  update(id: string, dto: UpdateRoleDto) {
    return this.http.put<RoleDto>(`${this.baseUrl}/${id}`, dto);
  }

  delete(id: string) {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }
}

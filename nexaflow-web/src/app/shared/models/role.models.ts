export interface RoleDto {
  id: string;
  name: string;
  description?: string;
  permissions: string[];
}

export interface CreateRoleDto {
  name: string;
  description?: string;
  permissions: string[];
}

export interface UpdateRoleDto {
  name: string;
  description?: string;
  permissions: string[];
}

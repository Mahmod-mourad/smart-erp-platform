import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { RoleService } from '../../../core/services/role.service';
import { RoleFormDialog } from './role-form-dialog/role-form-dialog';
import { RoleDto } from '../../../shared/models/role.models';

@Component({
  selector: 'app-role-list',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatTableModule, MatDialogModule, TranslatePipe],
  templateUrl: './role-list.html'
})
export class RoleList implements OnInit {
  public roleService = inject(RoleService);
  private dialog = inject(MatDialog);

  displayedColumns = ['name', 'description', 'permissionsCount', 'actions'];

  ngOnInit() {
    this.roleService.loadRoles();
    this.roleService.loadPermissions();
  }

  openDialog(role?: RoleDto) {
    const ref = this.dialog.open(RoleFormDialog, {
      width: '600px',
      data: role
    });

    ref.afterClosed().subscribe(res => {
      if (res) {
        if (role) {
          this.roleService.update(role.id, res).subscribe(() => this.roleService.loadRoles());
        } else {
          this.roleService.create(res).subscribe(() => this.roleService.loadRoles());
        }
      }
    });
  }

  deleteRole(id: string) {
    if (confirm('Are you sure you want to delete this role?')) {
      this.roleService.delete(id).subscribe(() => this.roleService.loadRoles());
    }
  }
}
